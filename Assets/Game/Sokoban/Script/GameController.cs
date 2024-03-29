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
    [UDictionary.Split(30, 70)]
    public TypeTileDictionary TypeTileAssociation;
    [Serializable]
    public class TypeTileDictionary : UDictionary<GameWorld.GridObjectType, TileBase> { }

    public int GenerationsToRun { get; set; } = 100;

    // Instructions UI reference
    public GameObject InstructionsTextObject;

    // Rules UI references
    public GameObject NewRuleSetup, NoActionsLeftHint;
    public GameObject RulesList, ActionsList;
    public GameObject ActionItemPrefab, RulePrefab;

    // Playback UI references
    public GameObject PlayButton;
    public GameObject PlaybackSpeedTextObject, GenerationNumberTextObject;

    //public GameWorld GameWorld { get { return gameWorld; } }

    private readonly GameWorld gameWorld;
    private TextMeshProUGUI instructionsTextMesh, generationNumberTextMesh, playbackSpeedTextMesh;

    private readonly string pythonExePath = Path.Combine(Application.streamingAssetsPath, "Python", "python.exe");
    private readonly string pythonScriptPath = Path.Combine(Application.streamingAssetsPath, "AI", "script.py");
    private readonly string aiOutFilePath = Path.Combine(Application.streamingAssetsPath, "AI", "ai-out.txt");
    private readonly string parametersJsonPath = Path.Combine(Application.streamingAssetsPath, "AI", "parameters.json");
    
    private const string aiOutEndLine = "END";

    private List<Level> levels = new();
    private int currentLevel = 1;

    private List<GameRule> rules = new();
    private Dictionary<GameRule.ActionType, GameObject> actionTypeToActionItem = new();

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

    private bool playing;
    private Process aiProcess = null;

    /* TODO 
     * playback speed
     * num generations to run
     * level picker
     */

    public GameController() : base()
    {
        gameWorld = new GameWorld(this);
    }

    void Start()
    {
        instructionsTextMesh = InstructionsTextObject.GetComponent<TextMeshProUGUI>();
        generationNumberTextMesh = GenerationNumberTextObject.GetComponent<TextMeshProUGUI>();
        playbackSpeedTextMesh = PlaybackSpeedTextObject.GetComponent<TextMeshProUGUI>();

        InitialiseActionList();
        InitialiseRulesList();
        LoadAllLevels();
        OpenLevel(currentLevel);

        PlayButton.GetComponent<Button>().onClick.AddListener(OnPlayButtonClicked);
        SetPlaybackSpeed(playbackSpeed);
        playbackSpeedTextMesh.text = playbackSpeed.ToString();
    }

    public void OpenLevel(int level)
    {
        if (level < 1 || level > levels.Count)
        {
            UnityEngine.Debug.LogError("GameController.OpenLevel(): Level " + level + " does not exist.");
            return;
        }

        gameWorld.SetLevel(levels[level-1]);
        currentLevel = level;
        UpdateInstructions();
    }

    public void AddNewRule(GameRule.ActionType actionType)
    {
        GameObject ruleItem = Instantiate(RulePrefab, RulesList.transform);
        GameRule rule = ruleItem.GetComponent<GameRule>();
        // Debug.Log("GameController.AddNewRule(): New rule successful? " + rule != null ? "True" : "False");
        rule.Init(actionType);
        rules.Add(rule);

        ruleItem.GetComponentInChildren<Button>().onClick.AddListener(() => RemoveRule(ruleItem, rule));

        actionTypeToActionItem[actionType].SetActive(false);
        
        // check if all actions are now in use and show hint if so
        if (actionTypeToActionItem.Values.All(item => !item.activeSelf))
            NoActionsLeftHint.SetActive(true);
    }

    public void RemoveRule(GameObject ruleObject, GameRule rule)
    {
        rules.Remove(rule);
        Destroy(ruleObject);

        actionTypeToActionItem[rule.Action].SetActive(true);
        NoActionsLeftHint.SetActive(false);
    }

    public void SetPlaybackSpeed(float newSpeed)
    {
        playbackSpeed = (int)newSpeed;
    }

    private void InitialiseActionList()
    {
        if (ActionsList == null)
        {
            UnityEngine.Debug.LogError("GameController.InitialiseActionList(): Action List not assigned.");
            return;
        }

        // Ensure the new action menu is inactive by default
        NewRuleSetup.SetActive(false);

        // Clear the list
        for (int i = ActionsList.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(ActionsList.transform.GetChild(i).gameObject);
        }

        // Add an item for each action type
        foreach (GameRule.ActionType action in Enum.GetValues(typeof(GameRule.ActionType)))
        {
            GameObject actionItem = Instantiate(ActionItemPrefab, ActionsList.transform);
            actionItem.GetComponentInChildren<TextMeshProUGUI>().SetText(action.ToDescription());
            actionItem.GetComponent<Button>().onClick.AddListener(() => {
                AddNewRule(action);
                NewRuleSetup.SetActive(false);
            });
            actionTypeToActionItem.Add(action, actionItem);
        }
    }

    private void InitialiseRulesList()
    {
        // Clear the list
        for (int i = RulesList.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(RulesList.transform.GetChild(i).gameObject);
        }

        // Load cached rules
        // TODO

        // If no cached rules, add a default rules
        // TODO
    }

    private void OnPlayButtonClicked()
    {
        if (!playing)
            StartPlayback();
        else
            EndPlayback();
    }

    private void StartPlayback()
    {
        UnityEngine.Debug.Log("GameController.StartPlayback(): Starting playback...");        
                        
        // Write parameters file
        string paramsJson = JsonUtility.ToJson(new AIParameters(currentLevel, GenerationsToRun, rules), true);
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

    private IEnumerator PlaybackLoop()
    {
        gameWorld.DebugPrintMapState();

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
        
        PlayButton.GetComponent<PlayButton>().Toggle();
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

            gameWorld.DebugPrintMapState();

            // Halt playback if the user sets speed to 0
            while (playbackSpeed == 0)
                yield return new WaitForSeconds(0.5f);

            yield return new WaitForSeconds(CurrentMoveDelay);
        }

        // linger on the final state of the generation for a bit longer
        yield return new WaitForSeconds(CurrentMoveDelay * 2);
    }
    
    private void EndPlayback()
    {
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
        gameWorld.ResetMapState();
    }

    private void UpdateInstructions()
    {
        if (instructionsTextMesh == null)
        {
            UnityEngine.Debug.LogError("GameController.UpdateInstructions(): Instructions text mesh not set.");
            return;
        }

        //instructionsTextMesh.SetText("Level " + levels[currentLevel-1].LevelNumber + "\n\n" + 
        //    "MapName: " + levels[currentLevel - 1].MapName + "\n\n" +
        //    "MapString: " + levels[currentLevel - 1].MapString + "\n\n" +
        //    "Instructios: " + levels[currentLevel - 1].Instructions);
        instructionsTextMesh.SetText(levels[currentLevel-1].Instructions);
    }

    private void LoadAllLevels()
    {
        // NOTE if we want to stick with WebGL, must use UnityWebRequest to load files
        // https://docs.unity3d.com/ScriptReference/Networking.UnityWebRequest.html

        int levelNum = 1;
        while (File.Exists(Level.LevelDirectory + levelNum))
        {
            levels.Add(new Level(levelNum));
            levelNum++;
        }
    }
}
