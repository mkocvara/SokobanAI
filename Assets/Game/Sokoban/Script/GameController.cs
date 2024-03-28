using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

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

    private readonly string pythonExePath = Application.streamingAssetsPath + "/Python/python.exe";
    private readonly string pythonScript = Application.streamingAssetsPath + "/AI/script.py";
    private readonly string outFilePath = Application.streamingAssetsPath + "/AI/out.txt";

    // TODO level picker + revamp level loading to use StreamingAssets (otherwise it won't work in a live build)

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
        /* TODO
         * Write a parameters file with generations, level number, and rules
         * Trigger python script                                                        +
         * Loop through results and update game world until terminating line is found
         */

        UnityEngine.Debug.Log("GameController.StartPlayback(): Starting playback...");

        /* // Debugging code
        string addLine = "\n\n Application.dataPath = " + Application.dataPath + 
                         "\n Application.absoluteURL = " + Application.absoluteURL + 
                         "\n Application.streamingAssetsPath = " + Application.streamingAssetsPath + 
                         "\n Runtime.PythonDLL = " + Application.streamingAssetsPath +  "/Python/python311.dll" +
                         "\n Application.temporaryCachePath = " + Application.temporaryCachePath +
                         "\n Application.persistentDataPath = " + Application.persistentDataPath + 
                         "\n Application.isEditor = " + Application.isEditor +
                         "\n Application.isMobilePlatform = " + Application.isMobilePlatform +
                         "\n Application.isConsolePlatform = " + Application.isConsolePlatform +
                         "\n Application.isBatchMode = " + Application.isBatchMode +
                         "\n Level.LevelDirectory = " + Level.LevelDirectory +
                         "\n File.Exists(Level.LevelDirectory + levelNum) = " + (File.Exists(Level.LevelDirectory + "1") ? "True" : "False")
                         ;

        addLine += "\n Directory.GetFiles(Application.streamingAssetsPath):";
        try
        {
            Directory.GetFiles("/StreamingAssets/").ToList().ForEach(file => addLine += "\n - " + file);
        }
        catch(Exception e)
        {
            addLine += "\n Exception: " + e.Message;
        }

        //addLine += "\n Directory.GetFiles(Level.LevelDirectory):";
        //Directory.GetFiles(Level.LevelDirectory).ToList().ForEach(file => addLine += "\n - " + file);

        instructionsTextMesh.SetText(instructionsTextMesh.text + addLine + "\n");*/
        
        
        // Write parameters file
        // TODO


        // Run the python script
        ProcessStartInfo start = new()
        {
            FileName = "\"" + pythonExePath + "\"",
            Arguments = "\"" + pythonScript + "\" \"" + outFilePath + "\"",
            UseShellExecute = true,
            RedirectStandardOutput = false
        };

        UnityEngine.Debug.Log("GameController.StartPlayback(): filename = " + start.FileName + "; args = " + start.Arguments);
        UnityEngine.Debug.Log("GameController.StartPlayback(): Starting python process...");
        Process.Start(start);

        // Read the output file and play back
        // TODO

        // Keeping the following variant in case of future need.
        /* TODO
         * Try putting files in StreamingAssets and see if they persist in the build    +
         * Retrieve the path to the python script from the StreamingAssets folder       +  
         * Read file into string    
         * Use string to execute python code
         * Maybe set a filepath for the resulting file
         * Alternatively read results directly?
         * !!! make it async
         */

        /*
        try
        {
            Installer.SetupPython();
            Runtime.PythonDLL = Application.streamingAssetsPath + "/Python/python311.dll";
            PythonEngine.Initialize(); // do this in Start() later
            dynamic sys = PyModule.Import("sys");
            UnityEngine.Debug.Log("GameController.StartPlayback(): Python version: " + sys.version);
        }
        catch (Exception e)
        {
           UnityEngine.Debug.LogError("GameController.StartPlayback(): Error setting up Python: " + e.Message);
        }
        
        using (Py.GIL())
        {
            //string code = "import os\r\n\r\nmessage = 'Hello World!'\r\nfilepath = os.path.dirname(os.path.realpath(__file__)) + '\\\\test.txt'\r\n\r\nwith open(file=filepath, mode='w') as f:\r\n    f.write(message)\r\n\r\nprint(f'\"{message}\" written in \"{filepath}\"')";
            //PythonEngine.Exec(code);

            string filePath = Application.streamingAssetsPath + "/Python/script.py";
            
            // add the directory to the path so that the script can import scripts from there
            //dynamic os = Py.Import("os");
            //dynamic sys = Py.Import("sys");
            //sys.path.append(os.path.dirname(os.path.expanduser(filePath)));

            PyObject fromFile = Py.Import(Path.GetFileNameWithoutExtension(filePath));
            fromFile.InvokeMethod("main");
        }
        */
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
