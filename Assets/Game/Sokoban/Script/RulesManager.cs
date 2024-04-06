using System;
using System.Collections.Generic;
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

    void Start()
    {
        InitialiseActionList();
        InitialiseRulesList();
        InitialiseRulesets();

        SaveRulesetButton.GetComponent<Button>().onClick.AddListener(
            () => {
                string name = SaveRulesetNameInput.GetComponent<TMP_InputField>().text;
                SaveRuleset(name);
            });
    }

    public void AddNewRule(GameRule.ActionType actionType, int reward = 0)
    {
        GameObject ruleItem = Instantiate(RulePrefab, RulesList.transform);
        GameRule rule = ruleItem.GetComponent<GameRule>();
        // Debug.Log("GameController.AddNewRule(): New rule successful? " + rule != null ? "True" : "False");
        rule.Init(actionType, reward);
        Rules.Add(rule);

        ruleItem.GetComponentInChildren<Button>().onClick.AddListener(() => RemoveRule(ruleItem, rule));

        actionTypeToActionItem[actionType].SetActive(false);

        // Check if all actions are now in use and show hint if so
        if (actionTypeToActionItem.Values.All(item => !item.activeSelf))
            NoActionsLeftHint.SetActive(true);
    }
    
    public void RemoveRule(GameObject ruleObject, GameRule rule)
    {
        Rules.Remove(rule);
        Destroy(ruleObject);

        actionTypeToActionItem[rule.Action].SetActive(true);
        NoActionsLeftHint.SetActive(false);
    }

    public void SaveRuleset(string name)
    {
        if (Rules.Count == 0)
        {
            Debug.LogWarning("GameController.SaveRules(): No rules to save.");
            return;

            // TODO: this would deserve better handling, ideally not
            // even letting the player try to save when they have no rules.
            // But time is running out and this is very low priority.
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
        // Auto save current rules (unless loading auto save)
        string autoSaveName = "AutoSaveRules";
        if (name != autoSaveName && Rules.Count != 0)
            SaveRuleset(autoSaveName); 

        // Wipe current rules
        foreach (GameRule rule in Rules)
            Destroy(rule.gameObject);
        Rules.Clear();

        // Load new rules
        Ruleset ruleset = rulesets.Find(r => r.RulesetName == name);
        if (ruleset != null)
        {
            SerializableGameRule[] rules = ruleset.SerializableRules;
            foreach (SerializableGameRule rule in rules)
                AddNewRule(rule.Action, rule.Reward);
        }
        else
        {
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