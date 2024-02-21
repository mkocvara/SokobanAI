using System;
using System.Collections.Generic;
using System.IO;
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
    public GameWorld GameWorld { get { return gameWorld; } }

    private List<Level> levels = new();
    private int currentLevel = 1;

    private List<GameRule> rules = new(); // TODO

    private readonly GameWorld gameWorld;

    public GameController() : base()
    {
        gameWorld = new GameWorld(this);
    }

    void Start()
    {
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
    }

    public void StartPlayback()
    {
        // TODO
        throw new NotImplementedException();
    }
}
