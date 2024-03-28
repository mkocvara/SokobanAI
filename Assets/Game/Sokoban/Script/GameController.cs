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

public class GameController : MonoBehaviour
{
    [UDictionary.Split(30, 70)]
    public TypeTileDictionary TypeTileAssociation;
    [Serializable]
    public class TypeTileDictionary : UDictionary<GameWorld.GridObjectType, TileBase> { }

    public int GenerationsToRun = 100;
    public float PlaybackSpeed = 1f;

    public GameObject InstructionsTextObject;

    public GameObject PlayButton;
    public GameObject NewRuleSetup, NoActionsLeftHint;
    public GameObject RulesList, ActionsList;
    public GameObject ActionItemPrefab, RulePrefab;

    public GameWorld GameWorld { get { return gameWorld; } }

    private List<Level> levels = new();
    private int currentLevel = 1;

    private List<GameRule> rules = new();
    private Dictionary<GameRule.ActionType, GameObject> actionTypeToActionItem = new();

    private readonly GameWorld gameWorld;
    private TextMeshProUGUI instructionsTextMesh;

    private readonly string pythonExePath = Path.Combine(Application.streamingAssetsPath, "Python", "python.exe");
    private readonly string pythonScriptPath = Path.Combine(Application.streamingAssetsPath, "AI", "script.py");
    private readonly string aiOutFilePath = Path.Combine(Application.streamingAssetsPath, "AI", "ai-out.txt");
    private readonly string parametersJsonPath = Path.Combine(Application.streamingAssetsPath, "AI", "parameters.json");
    
    private readonly string aiOutEndLine = "END";
    private readonly float baseMoveDelay = 0.25f;

    /* TODO 
     * Num of generation running (count lines in ai-out.txt and set a UI element)
     * stop running
     * level picker
     */

    public GameController() : base()
    {
        gameWorld = new GameWorld(this);
    }

    void Start()
    {
        instructionsTextMesh = InstructionsTextObject.GetComponent<TextMeshProUGUI>();
        InitialiseActionList();
        InitialiseRulesList();
        LoadAllLevels();
        OpenLevel(currentLevel);

        PlayButton.GetComponent<Button>().onClick.AddListener(StartPlayback);
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

    public void StartPlayback()
    {
        UnityEngine.Debug.Log("GameController.StartPlayback(): Starting playback...");        
        
        // Write parameters file
        string paramsJson = JsonUtility.ToJson(new AIParameters(currentLevel, GenerationsToRun, rules), true);
        File.WriteAllText(parametersJsonPath, paramsJson);

        // Delete the AI output file to avoid playing back an old run
        File.Delete(aiOutFilePath);

        // Run the python script
        ProcessStartInfo start = new()
        {
            FileName = "\"" + pythonExePath + "\"",
            Arguments = "\"" + pythonScriptPath + "\" \"" + aiOutFilePath + "\"",
            UseShellExecute = true,
            RedirectStandardOutput = false
        };

        UnityEngine.Debug.Log("GameController.StartPlayback(): filename = " + start.FileName + "; args = " + start.Arguments);
        UnityEngine.Debug.Log("GameController.StartPlayback(): Starting python process...");
        Process.Start(start);

        // Read the output file and play back
        StartCoroutine(Playback());
    }

    private IEnumerator Playback()
    {
        GameWorld.DebugPrintMapState();

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
            lines = File.ReadLines(aiOutFilePath); // TODO may have to be changed to ReadAllLines() if the "streaming" makes the file inaccessible by the python script

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
                // sleep for 2 seconds and try again
                yield return new WaitForSeconds(1);
                continue;
            }

            playLine = nextLine;
            yield return StartCoroutine(PlayGeneration(playLine));
            GameWorld.ResetMapState();
        }
        while (playLine != aiOutEndLine);

        // get line before the terminating line and play it if it hasn't been played yet
        nextLine = lines.SkipLast(1).Last();
        if (nextLine != playLine)
        {
            yield return StartCoroutine(PlayGeneration(playLine = nextLine));
            // linger even longer on the final state of the final generation 
            yield return new WaitForSeconds(baseMoveDelay * 3);
            GameWorld.ResetMapState();
        }
        UnityEngine.Debug.Log("GameController.StartPlayback(): Playback complete.");
    }

    private IEnumerator PlayGeneration(string playLine)
    {
        UnityEngine.Debug.Log("GameController.PlayGeneration(): Playing back line: " + playLine);

        foreach (char actionChar  in playLine)
        {
            switch (actionChar)
            {
                case 'U':
                    GameWorld.MakeMove(GameWorld.MoveDir.Up);
                    break;
                case 'D':
                    GameWorld.MakeMove(GameWorld.MoveDir.Down);
                    break;
                case 'L':
                    GameWorld.MakeMove(GameWorld.MoveDir.Left);
                    break;
                case 'R':
                    GameWorld.MakeMove(GameWorld.MoveDir.Right);
                    break;
                default:
                    UnityEngine.Debug.LogWarning("GameController.PlayGeneration(): Invalid action character: \'" + actionChar + "\'");
                    break;
            }

            GameWorld.DebugPrintMapState();

            float delay = baseMoveDelay + (1f - PlaybackSpeed) * baseMoveDelay;
            yield return new WaitForSeconds(delay);
        }

        // linger on the final state of the generation for a bit longer
        yield return new WaitForSeconds(baseMoveDelay * 2);
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
