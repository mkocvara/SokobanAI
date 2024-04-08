using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RulesManager : MonoBehaviour
{
    public GameObject NewRuleSetup, NoActionsLeftHint, NoRulesetsHint;
    public GameObject RulesList, ActionsList, RulesetsList;
    public GameObject RulePrefab, ActionItemPrefab, RulesetPrefab;
    public GameObject SaveRulesetButton, SaveRulesetNameInput;
    public GameObject LoadRulesets, LoadRulesetsDialog, RulesetDeleteDialog, RulesetDeleteButton;

    public List<GameRule> Rules { get; private set; } = new();

    private readonly Dictionary<GameRule.ActionType, GameObject> actionTypeToActionItem = new();

    private List<Ruleset> rulesets = new();

    private const string autoSaveRulesetName = "Auto Save";

    private readonly string levelSolutionsDirectory = Path.Combine(Application.streamingAssetsPath, "Levels");

    void Start()
    {
        InitialiseActionList();
        InitialiseRulesets();
        InitialiseRulesList();

        SaveRulesetButton.GetComponent<Button>().onClick.AddListener(
            () => {
                string name = SaveRulesetNameInput.GetComponent<TMP_InputField>().text;
                SaveRuleset(name);
            });
    }

    private void OnApplicationQuit()
    {
        AutoSaveRuleset();
    }

    public void AddNewRule(GameRule.ActionType actionType, int reward = 0)
    {
        GameObject ruleItem = Instantiate(RulePrefab, RulesList.transform);
        GameRule rule = ruleItem.GetComponent<GameRule>();
        // Debug.Log("GameController.AddNewRule(): New rule successful? " + rule != null ? "True" : "False");
        rule.Init(actionType, reward);
        Rules.Add(rule);

        ruleItem.GetComponentInChildren<Button>().onClick.AddListener(() => RemoveRule(rule));

        actionTypeToActionItem[actionType].SetActive(false);

        // Check if all actions are now in use and show hint if so
        if (actionTypeToActionItem.Values.All(item => !item.activeSelf))
            NoActionsLeftHint.SetActive(true);
    }

    public void RemoveRule(GameRule rule)
    {
        GameObject ruleObject = rule.gameObject;

        Rules.Remove(rule);
        Destroy(ruleObject);

        actionTypeToActionItem[rule.Action].SetActive(true);
        NoActionsLeftHint.SetActive(false);
    }

    public void RemoveRule(GameObject ruleObject)
    {
        RemoveRule(ruleObject.GetComponent<GameRule>());
    }

    public void SaveRuleset(string name)
    {
        if (Rules.Count == 0)
        {
            Debug.LogWarning("GameController.SaveRules(): No rules to save.");
            // return;

            // TODO: this would deserve better handling, ideally not
            // even letting the player try to save when they have no rules.
            // But time is running out and this is very low priority.
            // Let them do it for now, so that at least the behaviour is
            // as a user would expect.
        }

        Ruleset ruleset = rulesets.Find(r => r.RulesetName == name);
        if (ruleset != null)
            ruleset.SetRules(Rules);
        else
            ruleset = CreateRuleset(name);

        ruleset.SaveToFile();

        NoRulesetsHint.SetActive(false);
    }

    public void LoadRuleset(string name)
    {
        Ruleset ruleset = rulesets.Find(r => r.RulesetName == name);
        if (ruleset != null)
        {
            LoadRulesetInner(ruleset.SerializableRules);
        }
        else
        {
            if (name == autoSaveRulesetName)
                Debug.Log("GameController.LoadRules(): No auto saved rules.");
            else
                Debug.LogWarning("GameController.LoadRules(): Ruleset not found.");
            return;
        }
    }

    public void RemoveRuleset(string name)
    {
        //Debug.Log("GameController.RemoveRuleset(): Removing ruleset " + name);

        Ruleset ruleset = rulesets.Find(r => r.RulesetName == name);

        if (ruleset == null)
        {
            Debug.LogWarning("GameController.RemoveRuleset(): Ruleset not found.");
            return;
        }

        ruleset.DeleteFile();
        rulesets.Remove(ruleset);
        Destroy(ruleset.gameObject);

        NoRulesetsHint.SetActive(rulesets.Count == 0);
    }

    public void LoadLevelSolution(int levelNum)
    {
        string path = Path.Combine(levelSolutionsDirectory, levelNum + ".solution");
        Ruleset.SerializableRuleset? ruleset = Ruleset.LoadRulesetFromPath(path);
        if (ruleset == null)
        {
            Debug.LogWarning("GameController.SolveLevel(): No solution found for level " + levelNum);
            return;
        }

        LoadRulesetInner(ruleset.Value.Rules);
    }

    private void LoadRulesetInner(SerializableGameRule[] rules)
    {
        // Auto save current rules (unless loading auto save)
        if (name != autoSaveRulesetName && Rules.Count != 0)
            AutoSaveRuleset();

        // Wipe current rules
        while (Rules.Any())
            RemoveRule(Rules.First());

        // Load new rules
        foreach (SerializableGameRule rule in rules)
            AddNewRule(rule.Action, rule.Reward);
    }

    private void AutoSaveRuleset()
    {
        SaveRuleset(autoSaveRulesetName);
    }

    private void InitialiseActionList()
    {
        if (ActionsList == null)
        {
            Debug.LogError("GameController.InitialiseActionList(): Action List not assigned.");
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

        // Load auto saved rules
        LoadRuleset(autoSaveRulesetName);
    }

    private void InitialiseRulesets()
    {
        // Clear the list
        for (int i = RulesetsList.transform.childCount - 1; i >= 0; i--)
            Destroy(RulesetsList.transform.GetChild(i).gameObject);

        // Load saved rulesets and instantiate them
        foreach (Ruleset.SerializableRuleset ruleset in Ruleset.LoadAllSavedRulesets())
            CreateRuleset(ruleset.Name, ruleset.Rules);

        // Show hint if no rulesets
        NoRulesetsHint.SetActive(rulesets.Count == 0);
    }

    private Ruleset CreateRuleset(string name, SerializableGameRule[] serializableRules = null)
    {
        GameObject rulesetItem = Instantiate(RulesetPrefab, RulesetsList.transform);

        Ruleset rulesetComponent = rulesetItem.GetComponent<Ruleset>();
        if (serializableRules != null)
            rulesetComponent.Init(name, serializableRules, this);
        else
            rulesetComponent.Init(name, Rules, this);

        rulesets.Add(rulesetComponent);
        
        return rulesetComponent;
    }
}
