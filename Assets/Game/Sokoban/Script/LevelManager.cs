using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelManager : MonoBehaviour
{
    public GameObject InstructionsTextObject, LevelPicker, LevelsGrid, LevelSolvedHint;
    public GameObject LevelPrefab;

    public int CurrentLevelNumber { get; private set; } = -1;

    private GameController gameController;
    private GameWorld gameWorld;
    private TextMeshProUGUI instructionsTextMesh;

    private List<Level> levels = new();
    private Level CurrentLevel { get { return levels[CurrentLevelNumber - 1]; } }

    void Start()
    {
        gameController = FindObjectOfType<GameController>();
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
        if (CurrentLevelNumber == level)
            return;

        if (level < 1 || level > levels.Count)
        {
            Debug.LogWarning("GameController.OpenLevel(): Level " + level + " does not exist.");
            return;
        }

        gameController.EndPlayback();
        gameWorld.SetLevel(levels[level - 1]);

        CurrentLevelNumber = level;
        UpdateInstructions();
        LevelSolvedHint.SetActive(CurrentLevel.IsSolved);
    }

    public void OpenNextLevel()
    {
        if (CurrentLevelNumber < levels.Count)
        {
            OpenLevel(CurrentLevelNumber + 1);
        }
    }

    public void SetCurrentLevelSolved(bool solved)
    {
        CurrentLevel.SetSolved(solved);
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

    private void CreateLevel(int levelNum)
    {
        GameObject levelItem = Instantiate(LevelPrefab, LevelsGrid.transform);
        Level level = levelItem.GetComponent<Level>();
        level.Init(levelNum, LevelSolvedHint);
        levels.Add(level);

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
