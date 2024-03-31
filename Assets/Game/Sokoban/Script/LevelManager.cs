using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelManager : MonoBehaviour
{
    public List<Level> Levels { get; private set; } = new();
    public int CurrentLevelNumber { get; private set; } = 1;
    public Level CurrentLevel { get { return Levels[CurrentLevelNumber - 1]; } }

    // References
    public GameObject InstructionsTextObject, LevelPicker, LevelsGrid;
    public GameObject LevelPrefab;

    private GameWorld gameWorld;
    private TextMeshProUGUI instructionsTextMesh;

    void Start()
    {
        gameWorld = FindObjectOfType<GameWorld>();
        instructionsTextMesh = InstructionsTextObject.GetComponent<TextMeshProUGUI>();

        // Clear dummy level buttons
        for (int i = LevelsGrid.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(LevelsGrid.transform.GetChild(i).gameObject);
        }

        // ensure level picker starts hidden
        LevelPicker.SetActive(false); 

        LoadAllLevels();
        OpenLevel(1);
    }

    public void OpenLevel(int level)
    {
        if (level < 1 || level > Levels.Count)
        {
            Debug.LogWarning("GameController.OpenLevel(): Level " + level + " does not exist.");
            return;
        }

        gameWorld.SetLevel(Levels[level - 1]);
        CurrentLevelNumber = level;
        UpdateInstructions();
    }

    private void LoadAllLevels()
    {
        // NOTE if we want to stick with WebGL, must use UnityWebRequest to load files
        // https://docs.unity3d.com/ScriptReference/Networking.UnityWebRequest.html

        int levelNum = 1;
        while (File.Exists(Level.LevelDirectory + levelNum))
        {
            CreateLevel(levelNum);
            levelNum++;
        }
    }

    public void CreateLevel(int levelNum)
    {
        GameController gameController = FindObjectOfType<GameController>();

        GameObject levelItem = Instantiate(LevelPrefab, LevelsGrid.transform);
        Level level = levelItem.GetComponent<Level>();
        level.Init(levelNum);
        Levels.Add(level);

        levelItem.GetComponent<Button>().onClick.AddListener(() => {
            gameController.ChangeLevel(levelNum);
            LevelPicker.SetActive(false);
        });
    }

    private void UpdateInstructions()
    {
        if (instructionsTextMesh == null)
        {
            Debug.LogError("GameController.UpdateInstructions(): Instructions text mesh not set.");
            return;
        }

        //instructionsTextMesh.SetText("Level " + levels[currentLevel-1].LevelNumber + "\n\n" + 
        //    "MapName: " + levels[currentLevel - 1].MapName + "\n\n" +
        //    "MapString: " + levels[currentLevel - 1].MapString + "\n\n" +
        //    "Instructios: " + levels[currentLevel - 1].Instructions);

        instructionsTextMesh.SetText("<b>" + CurrentLevel.MapName + "</b>\n\n" + CurrentLevel.Instructions);
    }
}
