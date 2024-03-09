using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GameWorld
{
    public enum MoveDir
    {
        Up,
        Down,
        Left,
        Right
    }

    public enum GridObjectType
    {
        None,
        Wall,
        Box,
        Player,
        Mark,
        BoxOnMark,
        PlayerOnMark,
        Ground
    }

    private readonly Dictionary<char, GridObjectType> objectTypeSymbolPairs = new()
    {
        { '#', GridObjectType.Wall },
        { 'b', GridObjectType.Box },
        { 'p', GridObjectType.Player },
        { 'x', GridObjectType.Mark },
        { '.', GridObjectType.Ground },

        { 'B', GridObjectType.BoxOnMark },
        { 'P', GridObjectType.PlayerOnMark },

        { ' ', GridObjectType.None },
    };

    private readonly GameController gameController;

    private GridObjectType[,] mapState;
    private Tilemap tilemap;
    private Level currentLevel;
    private int2 playerPos;
    private int2 mapSize;

    public GameWorld(GameController gameController)
    {
        this.gameController = gameController;
    }

    public void SetLevel(Level level)
    {
        currentLevel = level;
        InitMapState();
        FitCameraToMap();
    }

    public void InitMapState()
    {
        string[] rows = currentLevel.MapString.Split('\n');

        int longestRow = rows.Select(r => r.Length)
                             .Max();

        for (int i = 0; i < rows.Length; i++)
        {
            if (rows[i].Length < longestRow)
                rows[i] = rows[i].PadRight(longestRow, ' ');
        }

        mapSize.x = rows[0].Length;
        mapSize.y = rows.Length;

        mapState = new GridObjectType[mapSize.x, mapSize.y];

        for (int i = 0; i < mapSize.x; i++)
        {
            for (int j = 0; j < mapSize.y; j++)
            {
                GridObjectType gridObjectType = GridObjectType.None;
                
                char symbol = rows[j][i];

                if (objectTypeSymbolPairs.ContainsKey(symbol))
                    gridObjectType = objectTypeSymbolPairs[symbol];

                if (gridObjectType == GridObjectType.Player)
                    playerPos = new(i, j);

                mapState[i, j] = gridObjectType;
            }
        }

        MakeMap();
    }

    private void MakeMap()
    {
        tilemap = GameObject.FindObjectOfType<Tilemap>();
        tilemap.ClearAllTiles();

        // Loop through the width of the map
        for (int x = 0; x < mapSize.x; x++)
        {
            // Loop through the height of the map
            for (int y = 0; y < mapSize.y; y++)
            {
                if (!gameController.TypeTileAssociation.ContainsKey(mapState[x, y]))
                    continue;

                TileBase tile = gameController.TypeTileAssociation[mapState[x,y]];
                
                // Invert the y axis because Unity's tilemap has the origin at the bottom left
                int tilePosX = x;
                int tilePosY = (mapSize.y - 1 - y);

                tilemap.SetTile(new Vector3Int(tilePosX, tilePosY, 0), tile);
            }
        }
    }

    private void FitCameraToMap()
    {
        int biggerSide = Mathf.Max(mapSize.x, mapSize.y);
        Camera.main.orthographicSize = biggerSide / 2.0f;
        Camera.main.transform.position = new Vector3(tilemap.size.x / 2.0f, tilemap.size.y / 2.0f, Camera.main.transform.position.z);
        
    }

    public void ResetMapState()
    {
        InitMapState();
    }

    public void MakeMove(MoveDir move)
    {
        int2 moveTo = playerPos;
        switch (move)
        {
            case MoveDir.Up:
                moveTo.y += 1;
                break;
            case MoveDir.Down:
                moveTo.y -= 1;
                break;
            case MoveDir.Left:
                moveTo.x -= 1;
                break;
            case MoveDir.Right:
                moveTo.x += 1;
                break;
        }

        switch (mapState[moveTo.x, moveTo.y])
        {
            case GridObjectType.Box:
            case GridObjectType.BoxOnMark:
                if (MoveBox(moveTo, moveTo + (moveTo - playerPos)))
                    MovePlayer(moveTo);
                break;

            case GridObjectType.Mark:
            case GridObjectType.Ground:
                MovePlayer(moveTo);
                break;

            case GridObjectType.Wall:
                break;
            default:
                Debug.LogError("GameWorld.MakeMove(): Unrecognised object.");
                break;
        }
    }

    private void MovePlayer(int2 moveTo)
    {
        mapState[moveTo.x, moveTo.y] = mapState[moveTo.x, moveTo.y] == GridObjectType.Mark ? GridObjectType.PlayerOnMark : GridObjectType.Player;
        GridObjectType underPlayer = mapState[playerPos.x, playerPos.y] == GridObjectType.PlayerOnMark ? GridObjectType.Mark : GridObjectType.Ground;
        mapState[playerPos.x, playerPos.y] = underPlayer;

        tilemap.SetTile(new Vector3Int(playerPos.x, playerPos.y, 0), gameController.TypeTileAssociation[underPlayer]);
        tilemap.SetTile(new Vector3Int(moveTo.x, moveTo.y, 0), gameController.TypeTileAssociation[mapState[moveTo.x, moveTo.y]]);

        playerPos = moveTo;
    }

    private bool MoveBox(int2 boxPos, int2 moveTo)
    {
        if (!(mapState[moveTo.x, moveTo.y] == GridObjectType.Mark || mapState[moveTo.x, moveTo.y] == GridObjectType.Ground))
            return false;

        mapState[moveTo.x, moveTo.y] = mapState[moveTo.x, moveTo.y] == GridObjectType.Mark ? GridObjectType.BoxOnMark : GridObjectType.Box;
        GridObjectType underBox = mapState[boxPos.x, boxPos.y] == GridObjectType.BoxOnMark ? GridObjectType.Mark : GridObjectType.Ground;
        mapState[boxPos.x, boxPos.y] = underBox;

        tilemap.SetTile(new Vector3Int(boxPos.x, boxPos.y, 0), gameController.TypeTileAssociation[underBox]);
        tilemap.SetTile(new Vector3Int(moveTo.x, moveTo.y, 0), gameController.TypeTileAssociation[mapState[moveTo.x, moveTo.y]]);

        return true;
    }
}
