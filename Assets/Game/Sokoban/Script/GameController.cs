using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Text;

public class GameController : MonoBehaviour
{
    [Header("Playback Settings")]
    [Min(0)]
    public float GenerationFinishedDelayMultiplier = 2.0f;
    [Min(0)]
    public float LevelSolvedDelay = 3.0f;

    [Header("Default Parameters")]
    [Min(0)]
    public int DefaultGenerationsToRun = 3000;
    [Min(0)]
    public int DefaultExplorationThreshold = 100;

    [Header("References")]
    // Playback UI references
    public GameObject PlayButtonObject;
    public GameObject PlaybackSpeedTextObject, PlaybackPlayingLabelTextObject, GenerationNumberTextObject;
    public GameObject GenerationsInputObject, ExplorationThresholdInputObject;

    // Other references
    public GameObject MainMenu;
    public GameObject LevelPicker;

    public bool PausePlayback { get { return MainMenu.activeSelf || LevelPicker.activeSelf || playbackSpeed == 0; } }

    public static readonly string SaveDataDirectory = Path.Combine(Application.streamingAssetsPath, "SavedData");
    public SavedData SavedData { get { return savedData ??= LoadSavedData(); } }

    private const string savedDataFileName = "SavedData.json";
    private readonly string savedDataPath = Path.Combine(SaveDataDirectory, savedDataFileName);

    private static readonly string pythonExePath = Path.Combine(Application.streamingAssetsPath, "Python", "python.exe");
    private static readonly string pythonScriptPath = Path.Combine(Application.streamingAssetsPath, "AI", "main.py");
    private static readonly string aiOutFilePath = Path.Combine(Application.streamingAssetsPath, "AI", "ai-out.txt");
    private static readonly string parametersJsonPath = Path.Combine(Application.streamingAssetsPath, "AI", "parameters.json");

    private static readonly string paramPathArg = $"--params-path=\"{parametersJsonPath}\"";
    private static readonly string outPathArg = $"--out-path=\"{aiOutFilePath}\"";
    private static readonly string levelsDirPathArg = $"--levels-path=\"{Level.LevelDirectory}\"";

    private static readonly string aiOutEndLine = "END";

    private PlayButton playButton;
    private TextMeshProUGUI generationNumberTextMesh, playbackSpeedTextMesh, playbackPlayingLabelTextMesh;

    private TMP_InputField GenerationsInput { get { return generationsInput ??= GenerationsInputObject.GetComponent<TMP_InputField>(); } }
    private TMP_InputField ExplorationThresholdInput { get { return explorationThresholdInput ??= ExplorationThresholdInputObject.GetComponent<TMP_InputField>(); } }
    private TMP_InputField generationsInput, explorationThresholdInput;

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
        { 1, 1.5f },
        { 2, 0.5f },
        { 3, 0.1f },
        { 4, 0.05f },
        { 5, 0.01f }
    };

    private int generationsToRun = -1, explorationThreshold = -1, cachedGenerationsToRun = -1;

    private bool playing;
    private Process aiProcess = null;
    private StringBuilder aiErrorOutput;

    private bool GenerationFinished { 
        get 
        { 
            return generationFinished; 
        } 
        set 
        { 
            generationFinished = value; 
            UpdatePlayingHint(); 
        } 
    }
    private bool generationFinished = false;

    private SavedData savedData;

    /* TODO LIST
     * reduce the packaged python to bare essentials
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
        playbackSpeedTextMesh.text = playbackSpeed.ToString();
        
        if (generationsToRun <= 0)
        SetNumGenerations(DefaultGenerationsToRun);
        if (explorationThreshold <= 0)
        SetExplorationThreshold(DefaultExplorationThreshold);
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

    private void OnApplicationQuit()
    {
        EndPlayback();
        SaveDataToFile();
    }

    public void SaveDataToFile()
    {
        if (!Directory.Exists(SaveDataDirectory))
        {
            Directory.CreateDirectory(SaveDataDirectory);
        }

        SavedData savedData = new();
        levelManager.SaveLevelData(ref savedData);
        string json = JsonUtility.ToJson(savedData, true);
        File.WriteAllText(savedDataPath, json);
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

    public void OnEditExplorationThreshold(string inputText)
    {
        try
        {
            explorationThreshold = int.Parse(inputText);
        }
        catch
        {
            explorationThreshold = 0;
        }
    }

    public void SetNumGenerations(int numGenerations)
    {
        generationsToRun = numGenerations;
        GenerationsInput.text = numGenerations.ToString();
    }

    public void SetExplorationThreshold(int threshold)
    {
        explorationThreshold = threshold;
        ExplorationThresholdInput.text = threshold.ToString();
    }

    public void QuitToDesktop()
    {
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
        
        cachedGenerationsToRun = generationsToRun;

        UpdatePlayingHint();
        playButton.SetPlaying(true);

        // Write parameters file
        string paramsJson = JsonUtility.ToJson(new AIParameters(levelManager.CurrentLevelNumber, generationsToRun, explorationThreshold, rulesManager.Rules), true);
        File.WriteAllText(parametersJsonPath, paramsJson);

        // Delete the AI output file to avoid playing back an old run
        File.Delete(aiOutFilePath);

        // Generation number on the UI
        SetCurrentGeneration(0);

        // Run the python script
        ProcessStartInfo start = new()
        {
            FileName = pythonExePath.SurroundWithQuotes(),
            Arguments = string.Join(" ", new string[] { pythonScriptPath.SurroundWithQuotes(), paramPathArg, outPathArg, levelsDirPathArg }),
            UseShellExecute = false,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        };

        UnityEngine.Debug.Log("GameController.StartPlayback(): filename = " + start.FileName + "; args = " + start.Arguments);
        UnityEngine.Debug.Log("GameController.StartPlayback(): Starting python process...");

        aiProcess = new() { StartInfo = start };
        aiErrorOutput = new();
        aiProcess.ErrorDataReceived += (sender, args) => aiErrorOutput.AppendLine(args.Data);
        aiProcess.Start();
        aiProcess.BeginErrorReadLine();
        
        // Read the output file and play back
        StartCoroutine(PlaybackLoop());
        playing = true;
    }

    public void EndPlayback()
    {
        if (!playing)
            return;

        if (aiErrorOutput != null)
        {
            string errString = aiErrorOutput.ToString();
            if (!string.IsNullOrWhiteSpace(errString))
                UnityEngine.Debug.LogError("GameController.StartPlayback(): Error running AI script:\n" + errString);
        }

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

        float timeoutTimer = 0.0f;
        const float fileOpenWaitTime = 0.5f;
        const float fileOpenTimeout = 10.0f;
        const float readLineWaitTime = 0.01f;
        const float readLineTimeout = 10.0f;


        // Check if the AI output file has been created and wait for it to be if not
        while (!File.Exists(aiOutFilePath))
        {
            yield return new WaitForSeconds(fileOpenWaitTime);
            timeoutTimer += fileOpenWaitTime;
            if (timeoutTimer >= fileOpenTimeout)
            {
                UnityEngine.Debug.LogError("GameController.StartPlayback(): AI output file not found.");
                EndPlayback();
                yield break;
            }
        }

        IEnumerable<string> lines;
        int currentGeneration = 0, previousGeneration = 0;
        string nextLine;

        timeoutTimer = 0.0f;

        while (true) // Will break when the terminating line is reached
        {
            // Read the next line from the AI output file
            try 
            { 
                lines = File.ReadLines(aiOutFilePath);
                nextLine = lines.Last();
                currentGeneration = lines.Count();

                // If the terminating line is reached, read the line before it and break
                if (nextLine == aiOutEndLine)
                {
                    nextLine = lines.SkipLast(1).Last();
                    currentGeneration--;
                    break;
                }
            }
            catch (IOException e) // File is being written to by the AI script
            {
                UnityEngine.Debug.LogWarning("GameController.StartPlayback(): " + e.Message);
                nextLine = null;
            }

            // Check if successful at reading the file, otherwise wait and try again
            if (string.IsNullOrEmpty(nextLine))
            {
                UnityEngine.Debug.Log("GameController.StartPlayback(): Waiting for AI output file access.");

                yield return new WaitForSeconds(readLineWaitTime); // Wait for its turn to read the file

                timeoutTimer += readLineWaitTime;
                if (timeoutTimer >= readLineTimeout) // Waited too long
                {
                    UnityEngine.Debug.LogError("GameController.StartPlayback(): AI output file is empty.");
                    EndPlayback();
                    yield break;
                }
                continue;
            }

            // Continue if successfully read the next line...

            // If the next line is the same as the current line, wait & try again
            if (currentGeneration == previousGeneration)
            {
                yield return new WaitForSeconds(readLineWaitTime);

                timeoutTimer += readLineWaitTime;
                if (timeoutTimer >= readLineTimeout) // Waited too long
                {
                    UnityEngine.Debug.LogError("GameController.StartPlayback(): AI output file is empty.");
                    EndPlayback();
                    yield break;
                }
                continue;
            }

            timeoutTimer = 0.0f;

            previousGeneration = currentGeneration;
            SetCurrentGeneration(currentGeneration);
            yield return StartCoroutine(PlayGeneration(nextLine));

            gameWorld.ResetMapState();
        }

        // Play the final generation if it hasn't been played yet
        if (currentGeneration != previousGeneration)
        {
            SetCurrentGeneration(currentGeneration); // -1 to accound for the terminating line
            yield return StartCoroutine(PlayGeneration(nextLine));
            yield return new WaitForSeconds(LevelSolvedDelay - (CurrentMoveDelay * GenerationFinishedDelayMultiplier)); 
                // - (CurrentMoveDelay * GenerationFinishedDelayMultiplier) because it has already been waited for
                // (Might have waited more or less if the playback speed has changed during the wait, but that's trivial)
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

        GenerationFinished = false;

        for (int i = 0; i < playLine.Count(); i++)
        {
            char actionChar = playLine[i];

            while (PausePlayback)
            {
                UpdatePlayingHint();
                yield return new WaitForSeconds(0.1f);
            }

            UpdatePlayingHint();

            switch (actionChar)
            {
                case 'U':
                    gameWorld.MakeMove(GameWorld.MoveDir.Up);
                    break;
                case 'D':
                    gameWorld.MakeMove(GameWorld.MoveDir.Down);
                    break;
                case 'L':
                    gameWorld.MakeMove(GameWorld.MoveDir.Left);
                    break;
                case 'R':
                    gameWorld.MakeMove(GameWorld.MoveDir.Right);
                    break;
                default:
                    UnityEngine.Debug.LogWarning("GameController.PlayGeneration(): Invalid action character: \'" + actionChar + "\'");
                    break;
            }

            // gameWorld.DebugPrintMapState();

            CheckLevelSolved();

            if (i == playLine.Count() - 1)
                GenerationFinished = true;

            yield return new WaitForSeconds(CurrentMoveDelay);
        }

        // linger on the final state of the generation for a bit longer
        yield return new WaitForSeconds(Math.Max(CurrentMoveDelay * (GenerationFinishedDelayMultiplier - 1), 0));
    }

    private bool CheckLevelSolved()
    {
        if (!gameWorld.IsLevelSolved())
            return false;

        UnityEngine.Debug.Log("GameController.CheckLevelSolved(): Level " + levelManager.CurrentLevelNumber + " completed.");
        levelManager.SetCurrentLevelSolved(true);

        return true;
    }

    private void UpdatePlayingHint()
    {
        if (PausePlayback)
            playbackPlayingLabelTextMesh.text = "Paused.";
        else
            playbackPlayingLabelTextMesh.text = GenerationFinished ? "Finished." : "Playing...";
    }

    private SavedData LoadSavedData()
    {
        if (!File.Exists(savedDataPath))
            return new SavedData();

        string json = File.ReadAllText(savedDataPath);
        return JsonUtility.FromJson<SavedData>(json);
    }
}
