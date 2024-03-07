using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GameController : MonoBehaviour
{
    [UDictionary.Split(30, 70)]
    public TypeTileDictionary TypeTileAssociation;
    [Serializable]
    public class TypeTileDictionary : UDictionary<GameWorld.GridObjectType, TileBase> { }

    public int GenerationsToRun = 100;
    public float PlaybackSpeed = 1f;

    public GameObject InstructionsTextObject;

    public GameWorld GameWorld { get { return gameWorld; } }

    private List<Level> levels = new();
    private int currentLevel = 1;

    private List<GameRule> rules = new(); // TODO

    private readonly GameWorld gameWorld;
    private TextMeshProUGUI instructionsTextMesh;

    public GameController() : base()
    {
        gameWorld = new GameWorld(this);
    }

    void Start()
    {
        instructionsTextMesh = InstructionsTextObject.GetComponent<TextMeshProUGUI>();

        LoadAllLevels();
        OpenLevel(currentLevel);
    }

    void Update()
    {
        
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

    public void OpenLevel(int level)
    {
        gameWorld.SetLevel(levels[level-1]);
        currentLevel = level;
        UpdateInstructions();
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

    public void StartPlayback()
    {
        // TODO
        throw new NotImplementedException();
    }
}
