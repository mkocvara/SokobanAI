using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelManager : MonoBehaviour
{
    public GameObject InstructionsTextObject, InstructionsScrollView;
    public GameObject LevelPicker, LevelLayout, LevelSolvedHint;
    public GameObject LevelPrefab;

    public int CurrentLevelNumber { get; private set; } = -1;

    private GameController gameController;
    private GameWorld gameWorld;
    private RulesManager rulesManager;
    private ProgressionManager progressionManager;

    private TextMeshProUGUI instructionsTextMesh;
    private ScrollRect instructionsScrollRect;

    private List<Level> levels = new();
    private Level CurrentLevel { get { return levels[CurrentLevelNumber - 1]; } }

    void Start()
    {
        gameController = FindObjectOfType<GameController>();
        rulesManager = FindObjectOfType<RulesManager>();
        gameWorld = FindObjectOfType<GameWorld>();
        progressionManager = FindObjectOfType<ProgressionManager>();

        instructionsTextMesh = InstructionsTextObject.GetComponent<TextMeshProUGUI>();
        instructionsScrollRect = InstructionsScrollView.GetComponent<ScrollRect>();

        // Clear dummy level buttons
        for (int i = LevelLayout.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(LevelLayout.transform.GetChild(i).gameObject);
        }

        // ensure level picker starts hidden
        LevelPicker.SetActive(false); 

        LoadAllLevels();
        LoadSavedData(gameController.SavedData);
        OpenFirstUnsolvedLevel();
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
        progressionManager.SetUpLevel(level);
        CurrentLevelNumber = level;
        LevelSolvedHint.SetActive(CurrentLevel.IsSolved);
        UpdateInstructions();
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

    public void LoadSolutionForCurrentLevel()
    {
        rulesManager.LoadLevelSolution(CurrentLevelNumber);
    }

    /// <summary>
    /// Populate level data in a SavedData object.
    /// </summary>
    /// <param name="savedData">SavedData object, which will have its level data set.</param>
    public void SaveLevelData(ref SavedData savedData)
    {
        savedData ??= new SavedData();
        savedData.LevelsSolved = new bool[levels.Count];
        for (int i = 0; i < levels.Count; i++)
        {
            savedData.LevelsSolved[i] = levels[i].IsSolved;
        }
    }

    public void LoadSavedData(SavedData savedData)
    {
        if (savedData == null)
        {
            Debug.LogWarning("GameController.LoadSavedData(): No saved data.");
            return;
        }

        for (int i = 0; i < levels.Count; i++)
        {
            levels[i].SetSolved(savedData.LevelsSolved[i]);
        }
    }

    public void ResetSolvedLevels()
    {
        levels.ForEach(l => l.SetSolved(false)); 
    }

    private void OpenFirstUnsolvedLevel()
    {
        Level firstUnsolved = levels.FirstOrDefault(l => !l.IsSolved);
        int levelToOpen = firstUnsolved != null ? firstUnsolved.LevelNumber : levels.Count;
        OpenLevel(levelToOpen);
    }

    private void LoadAllLevels()
    {
        // NOTE if we want to stick with WebGL, must use UnityWebRequest to load files
        // https://docs.unity3d.com/ScriptReference/Networking.UnityWebRequest.html

        int levelNum = 1;
        while (File.Exists(Path.Combine(Level.LevelDirectory, Level.GetFileNameFromLevelNumber(levelNum))))
        {
            CreateLevel(levelNum);
            levelNum++;
        }
    }

    private void CreateLevel(int levelNum)
    {
        GameObject levelItem = Instantiate(LevelPrefab, LevelLayout.transform);
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

        instructionsTextMesh.SetText($"<b>Level {CurrentLevel.LevelNumber}: {CurrentLevel.MapName}</b>\n\n{CurrentLevel.Instructions}");
        instructionsScrollRect.verticalNormalizedPosition = 1.0f;
    }
}
