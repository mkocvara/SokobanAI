using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

    public GameObject NewRuleSetup;
    public GameObject RulesList, ActionsList;
    public GameObject ActionItemPrefab, RulePrefab;

    public GameWorld GameWorld { get { return gameWorld; } }

    private List<Level> levels = new();
    private int currentLevel = 1;

    private List<GameRule> rules = new();
    private Dictionary<GameRule.ActionType, GameObject> actionTypeToActionItem = new();

    private readonly GameWorld gameWorld;
    private TextMeshProUGUI instructionsTextMesh;

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
    }

    private void InitialiseActionList()
    {
        if (ActionsList == null)
        {
            Debug.LogError("GameController: Action List not assigned.");
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
        gameWorld.SetLevel(levels[level-1]);
        currentLevel = level;
        UpdateInstructions();
    }

    public void AddNewRule(GameRule.ActionType actionType)
    {
        GameObject ruleItem = Instantiate(RulePrefab, RulesList.transform);
        GameRule rule = ruleItem.GetComponent<GameRule>();
        // Debug.Log("GameController: New rule successful? " + rule != null ? "True" : "False");
        rule.Init(actionType);
        rules.Add(rule);

        ruleItem.GetComponentInChildren<Button>().onClick.AddListener(() => RemoveRule(ruleItem, rule));

        actionTypeToActionItem[actionType].SetActive(false);
    }

    public void RemoveRule(GameObject ruleObject, GameRule rule)
    {
        rules.Remove(rule);
        Destroy(ruleObject);
        actionTypeToActionItem[rule.Action].SetActive(true);
    }

    public void StartPlayback()
    {
        // TODO
        throw new NotImplementedException();
    }

    private void UpdateInstructions()
    {
        if (instructionsTextMesh is null)
        {
            Console.WriteLine("Error: Instructions text mesh not set.");
            return;
        }

        instructionsTextMesh.SetText(levels[currentLevel-1].Instructions);
    }

    private void LoadAllLevels()
    {
        int levelNum = 1;
        while (File.Exists(Level.LevelDirectory + levelNum))
        {
            levels.Add(new Level(levelNum));
            levelNum++;
        }
    }
}
