using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using System.Collections;
using static GameWorld;

public class GameController : MonoBehaviour
{
    public bool PausePlayback { get 
        {
            bool paused = MainMenu.activeSelf || LevelPicker.activeSelf || playbackSpeed == 0;
            playbackPlayingLabelTextMesh.text = paused ? "Paused." : "Playing...";
            return paused; 
        } 
    }

    // Playback UI references
    public GameObject PlayButtonObject;
    public GameObject PlaybackSpeedTextObject, PlaybackPlayingLabelTextObject, GenerationNumberTextObject;

    // Other references
    public GameObject MainMenu;
    public GameObject LevelPicker;

    private PlayButton playButton;
    private TextMeshProUGUI generationNumberTextMesh, playbackSpeedTextMesh, playbackPlayingLabelTextMesh;

    private readonly string pythonExePath = Path.Combine(Application.streamingAssetsPath, "Python", "python.exe");
    private readonly string pythonScriptPath = Path.Combine(Application.streamingAssetsPath, "AI", "script.py");
    private readonly string aiOutFilePath = Path.Combine(Application.streamingAssetsPath, "AI", "ai-out.txt");
    private readonly string parametersJsonPath = Path.Combine(Application.streamingAssetsPath, "AI", "parameters.json");
    
    private const string aiOutEndLine = "END";

    private GameWorld gameWorld;
    private LevelManager levelManager;
    private RulesManager rulesManager;

    private float CurrentMoveDelay { 
        get 
        { 
            return playbackSpeedToMoveDelay.Keys.Contains(playbackSpeed) 
                ? playbackSpeedToMoveDelay[playbackSpeed] 
                : playbackSpeedToMoveDelay[playbackSpeedToMoveDelay.Count()]; 
        } 
    }

    private int playbackSpeed = 3;
    private readonly Dictionary<int, float> playbackSpeedToMoveDelay = new() {
        { 1, 3.0f },
        { 2, 1.0f },
        { 3, 0.5f },
        { 4, 0.25f },
        { 5, 0.1f }
    };

    private int generationsToRun = 100;

    private bool playing;
    private Process aiProcess = null;

    /* TODO 
     * Integrate actual AI script   -
     * solve level button           .
     * level completion             -
     * Fix UI not scaling right
     * On quit: auto save ruleset, save level completion status
     */

    void Start()
    {
        generationNumberTextMesh = GenerationNumberTextObject.GetComponent<TextMeshProUGUI>();
        playbackSpeedTextMesh = PlaybackSpeedTextObject.GetComponent<TextMeshProUGUI>();
        playbackPlayingLabelTextMesh = PlaybackPlayingLabelTextObject.GetComponent<TextMeshProUGUI>();
        playButton = PlayButtonObject.GetComponent<PlayButton>();

        gameWorld = FindObjectOfType<GameWorld>();
        levelManager = FindObjectOfType<LevelManager>();
        rulesManager = FindObjectOfType<RulesManager>();

        PlayButtonObject.GetComponent<Button>().onClick.AddListener(OnPlayButtonClicked);
        SetPlaybackSpeed(playbackSpeed);
        playbackSpeedTextMesh.text = playbackSpeed.ToString();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && !LevelPicker.activeSelf && !MainMenu.activeSelf)
            OnPlayButtonClicked();

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (LevelPicker.activeSelf)
                LevelPicker.SetActive(false);
            else
                MainMenu.SetActive(!MainMenu.activeSelf);
        }
    }

    public void SetPlaybackSpeed(float newSpeed)
    {
        playbackSpeed = (int)newSpeed;
    }

    public void OnEditNumGenerations(string inputText)
    {
        try
        {
            generationsToRun = int.Parse(inputText);
        }
        catch
        {
            generationsToRun = 0;
        }
    }

    public void QuitToDesktop()
    {
        EndPlayback();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif

        Application.Quit();
    }

    public void ChangeLevel(int levelNumber)
    {
        EndPlayback();
        levelManager.OpenLevel(levelNumber);
    }

    public void StartPlayback()
    {
        if (playing)
            return;

        UnityEngine.Debug.Log("GameController.StartPlayback(): Starting playback...");        
        
        playButton.SetPlaying(true);

        // Write parameters file
        string paramsJson = JsonUtility.ToJson(new AIParameters(levelManager.CurrentLevelNumber, generationsToRun, rulesManager.Rules), true);
        File.WriteAllText(parametersJsonPath, paramsJson);

        // Delete the AI output file to avoid playing back an old run
        File.Delete(aiOutFilePath);

        // Generation number on the UI
        SetCurrentGeneration(0);

        // Run the python script
        ProcessStartInfo start = new()
        {
            FileName = "\"" + pythonExePath + "\"",
            Arguments = "\"" + pythonScriptPath + "\" \"" + aiOutFilePath + "\"",
            UseShellExecute = true,
            RedirectStandardOutput = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        };

        UnityEngine.Debug.Log("GameController.StartPlayback(): filename = " + start.FileName + "; args = " + start.Arguments);
        UnityEngine.Debug.Log("GameController.StartPlayback(): Starting python process...");
        aiProcess = Process.Start(start);

        // Read the output file and play back
        StartCoroutine(PlaybackLoop());
        playing = true;
    }

    public void EndPlayback()
    {
        if (!playing)
            return;

        if (aiProcess != null)
        {
            aiProcess.Refresh();
            if (!aiProcess.HasExited)
            {
                aiProcess.Kill();
                aiProcess.Close();
            }

            aiProcess = null;
        }

        StopAllCoroutines();
        playing = false;
        playButton.SetPlaying(false);

        gameWorld.ResetMapState();
    }

    private void OnPlayButtonClicked()
    {
        if (!playing)
            StartPlayback();
        else
            EndPlayback();
    }

    private IEnumerator PlaybackLoop()
    {
        // gameWorld.DebugPrintMapState();

        IEnumerable<string> lines;
        string playLine = "", nextLine;

        int waitCounter = 0;
        while (!File.Exists(aiOutFilePath))
        {
            yield return new WaitForSeconds(2);
            waitCounter++;
            if (waitCounter > 10)
            {
                UnityEngine.Debug.LogError("GameController.StartPlayback(): AI output file not found.");
                yield break;
            }
        }

        waitCounter = 0;

        do
        {
            lines = File.ReadLines(aiOutFilePath); 

            if (lines == null || !lines.Any())
            {
                yield return new WaitForSeconds(1);
                waitCounter++;
                if (waitCounter > 10)
                {
                    UnityEngine.Debug.LogError("GameController.StartPlayback(): AI output file is empty.");
                    yield break;
                }
                continue;
            }

            waitCounter = 0;

            nextLine = lines.Last();

            // break if the terminating line is reached 
            if (nextLine == aiOutEndLine)
                break;

            if (nextLine == playLine)
            {
                // sleep for a second and try again
                yield return new WaitForSeconds(1);
                continue;
            }

            playLine = nextLine;
            SetCurrentGeneration(lines.Count());
            yield return StartCoroutine(PlayGeneration(playLine));

            gameWorld.ResetMapState();
        }
        while (playLine != aiOutEndLine);

        // get line before the terminating line and play it if it hasn't been played yet
        nextLine = lines.SkipLast(1).Last();
        if (nextLine != playLine)
        {
            SetCurrentGeneration(lines.Count() - 1); // -1 to accound for the terminating line
            yield return StartCoroutine(PlayGeneration(playLine = nextLine));

            // linger even longer on the final state of the final generation 
            yield return new WaitForSeconds(CurrentMoveDelay * 3);
        }

        UnityEngine.Debug.Log("GameController.StartPlayback(): Playback complete.");
        
        EndPlayback();
    }

    private void SetCurrentGeneration(int genNum)
    {
        //UnityEngine.Debug.Log("GameController.StartPlayback(): Playing back generation " + generation + "...");

        if (genNum == 0)
        {
            generationNumberTextMesh.text = "-";
            return;
        }

        generationNumberTextMesh.text = genNum.ToString();
    }

    private IEnumerator PlayGeneration(string playLine)
    {
        UnityEngine.Debug.Log("GameController.PlayGeneration(): Playing back line: " + playLine);

        foreach (char actionChar  in playLine)
        {
            switch (actionChar)
            {
                case 'U':
                    gameWorld.MakeMove(MoveDir.Up);
                    break;
                case 'D':
                    gameWorld.MakeMove(MoveDir.Down);
                    break;
                case 'L':
                    gameWorld.MakeMove(MoveDir.Left);
                    break;
                case 'R':
                    gameWorld.MakeMove(MoveDir.Right);
                    break;
                default:
                    UnityEngine.Debug.LogWarning("GameController.PlayGeneration(): Invalid action character: \'" + actionChar + "\'");
                    break;
            }

            // gameWorld.DebugPrintMapState();

            // Halt playback if the user sets speed to 0
            while (PausePlayback)
                yield return new WaitForSeconds(0.5f);

            // If the level is solved, handle that, linger on the solved state, then stop playing this generation.
            if (CheckLevelSolved())
            {
                yield return new WaitForSeconds(CurrentMoveDelay * 3);
                yield break;
            }

            yield return new WaitForSeconds(CurrentMoveDelay);
        }

        // linger on the final state of the generation for a bit longer
        yield return new WaitForSeconds(CurrentMoveDelay * 2);
    }

    private bool CheckLevelSolved()
    {
        if (!gameWorld.IsLevelSolved())
            return false;

        UnityEngine.Debug.Log("GameController.CheckLevelSolved(): Level " + levelManager.CurrentLevelNumber + " completed.");
        levelManager.SetCurrentLevelSolved(true);

        return true;
    }
}
