using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class Ruleset : MonoBehaviour
{

    [Serializable]
    public struct SerializableRuleset
    {
        public string Name;
        public SerializableGameRule[] Rules;

        public SerializableRuleset(string name, SerializableGameRule[] rules)
        {
            Name = name;
            Rules = rules;
        }

        public SerializableRuleset(Ruleset ruleset)
        {
            Name = ruleset.RulesetName;
            Rules = ruleset.SerializableRules;
        }
    }

    public GameObject TextObject, LoadButton, DeleteButton;

    public string RulesetName { get; set; }
    public SerializableGameRule[] SerializableRules { get; private set; }

    private TextMeshProUGUI textMesh;

    private static readonly string saveDirectory = Path.Combine(GameController.SaveDataDirectory, "Rulesets");
    private string FileName { get { return RulesetName.ToValidFileName('_', true) + ".json"; } }
    private string JsonPath { get { return Path.Combine(saveDirectory, FileName); } }

    private bool initialised = false;

    void Start()
    {
        // Hide until initialised
        if (!initialised) gameObject.SetActive(false);
    }

    public void Init(string name, SerializableGameRule[] rules, RulesManager rulesManager)
    {
        SerializableRules = rules;
        InitInner(name, rulesManager);
    }

    public void Init(string name, List<GameRule> rules, RulesManager rulesManager)
    {
        SetRules(rules);
        InitInner(name, rulesManager);
    }

    public void SetRules(List<GameRule> rules)
    {
        SerializableRules = rules.ToSerializable();
    }  

    public void SaveToFile()
    {
        if (!Directory.Exists(saveDirectory))
        {
            Directory.CreateDirectory(saveDirectory);
        }

        SerializableRuleset serialisable = new(this);
        string json = JsonUtility.ToJson(serialisable, true);
        File.WriteAllText(JsonPath, json);
    }

    public void DeleteFile()
    {
        if (File.Exists(JsonPath))
            File.Delete(JsonPath);
    }

    public static List<SerializableRuleset> LoadAllSavedRulesets()
    {
        List<SerializableRuleset> rulesets = new();

        if (!Directory.Exists(saveDirectory))
            return rulesets;

        string[] files = Directory.GetFiles(saveDirectory);
        IEnumerable<string> jsonFiles = files.Where(f => f.TakeLast(5).ToArray().ArrayToString() == ".json");
        foreach (string file in jsonFiles)
        {
            SerializableRuleset? ruleset = LoadRulesetFromPath(file);

            if (ruleset == null)
                continue;

            rulesets.Add(ruleset.Value);
        }

        return rulesets;
    }

    public static SerializableRuleset? LoadRulesetFromPath(string path)
    {
        if (!File.Exists(path))
            return null;

        string json = File.ReadAllText(path);

        try
        {
            return JsonUtility.FromJson<SerializableRuleset>(json);
        }
        catch (Exception e)
        {
            Debug.LogWarning("Ruleset.LoadAllSavedRulesets(): Failed to load ruleset from \"" + path + "\"; Exception: " + e.Message);
            return null;
        }
    }

    private void InitInner(string name, RulesManager rulesManager)
    {
        textMesh = TextObject.GetComponent<TextMeshProUGUI>();

        RulesetName = name;
        textMesh.text = name;

        LoadButton.GetComponent<Button>().onClick.AddListener(() => {
            rulesManager.LoadRuleset(RulesetName);
            rulesManager.LoadRulesets.SetActive(false);
        });

        
        DeleteButton.GetComponent<Button>().onClick.AddListener(() =>
        {
            rulesManager.LoadRulesetsDialog.SetActive(false);
            rulesManager.RulesetDeleteDialog.SetActive(true);

            Button deleteButton = rulesManager.RulesetDeleteButton.GetComponent<Button>();
            UnityAction callback = () => rulesManager.RemoveRuleset(RulesetName);

            deleteButton.onClick.AddListener(callback);
            deleteButton.onClick.AddListener(() =>
                deleteButton.onClick.RemoveListener(callback)
            );
        });

        gameObject.SetActive(initialised = true);
    }
}