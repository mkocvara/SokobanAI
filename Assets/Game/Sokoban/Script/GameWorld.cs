using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GameWorld : MonoBehaviour
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

    private class MapState
    {
        public int2 PlayerPos { get; set; }
        public int2 MapSize { get; set; }
        public Tilemap Tilemap { get; private set; }

        private GridObjectType[,] state;
        private readonly GameWorld gameWorld;

        public MapState(GameWorld gameWorld, int2 mapSize)
        {
            MapSize = mapSize;
            state = new GridObjectType[mapSize.y, mapSize.x];
            this.gameWorld = gameWorld;

            InitMap();
        }

        public MapState(GameWorld gameWorld, int mapSizeX, int mapSizeY) : this(gameWorld, new int2(mapSizeX, mapSizeY)) { }

        public void SetGridObject(GridObjectType gridObject, int x, int y)
        {
            state[y, x] = gridObject;

            if (gridObject == GridObjectType.None)
                return;

            // Set tile on the tilemap
            if (!gameWorld.TypeTileAssociation.ContainsKey(gridObject))
            {
                Debug.LogError("MapState.SetGridObject(): Can't find tile associated with action \'" + gridObject.ToDescription() + "\'");
                return;
            }

            TileBase tile = gameWorld.TypeTileAssociation[gridObject];

            // > Invert the y axis because Unity's tilemap has the origin at the bottom left
            int tilePosX = x;
            int tilePosY = (MapSize.y - 1 - y);

            Tilemap.SetTile(new Vector3Int(tilePosX, tilePosY, 0), tile);
        }

        public void SetGridObject(GridObjectType gridObject, int2 pos)
        {
            SetGridObject(gridObject, pos.x, pos.y);
        }

        public GridObjectType GetGridObject(int x, int y)
        {
            return state[y, x];
        }

        public GridObjectType GetGridObject(int2 pos)
        {
            return GetGridObject(pos.x, pos.y);
        }

        private void InitMap()
        {
            Tilemap = GameObject.FindObjectOfType<Tilemap>();
            Tilemap.ClearAllTiles();
        }
    }

    // Associate a type of object with a tile in editor
    [UDictionary.Split(30, 70)]
    public TypeTileDictionary TypeTileAssociation;
    [Serializable]
    public class TypeTileDictionary : UDictionary<GridObjectType, TileBase> { }

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

    private MapState mapState;
    private Level currentLevel;

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

        mapState = new(this, rows[0].Length, rows.Length);

        for (int x = 0; x < mapState.MapSize.x; x++)
        {
            for (int y = 0; y < mapState.MapSize.y; y++)
            {
                GridObjectType gridObjectType = GridObjectType.None;
                
                char symbol = rows[y][x]; // x and y are swapped because of how multidimentional arrays are represented differs from the map's coordinate system

                if (objectTypeSymbolPairs.ContainsKey(symbol))
                    gridObjectType = objectTypeSymbolPairs[symbol];

                if (gridObjectType == GridObjectType.Player)
                    mapState.PlayerPos = new(x, y);

                mapState.SetGridObject(gridObjectType, x, y);
            }
        }

        DebugPrintMapState();
    }

    public void ResetMapState()
    {
        InitMapState();
    }

    public void MakeMove(MoveDir move)
    {
        int2 moveTo = mapState.PlayerPos;
        switch (move)
        {
            case MoveDir.Up:
                moveTo.y -= 1;
                break;
            case MoveDir.Down:
                moveTo.y += 1;
                break;
            case MoveDir.Left:
                moveTo.x -= 1;
                break;
            case MoveDir.Right:
                moveTo.x += 1;
                break;
        }

        switch (mapState.GetGridObject(moveTo.x, moveTo.y))
        {
            case GridObjectType.Box:
            case GridObjectType.BoxOnMark:
                if (MoveBox(moveTo, moveTo + (moveTo - mapState.PlayerPos)))
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

#if DEBUG
    public void DebugPrintMapState()
    {
        string debugMapState = "Map State: ";
        for (int y = 0; y < mapState.MapSize.y; y++)
        {
            debugMapState += "\n";
            for (int x = 0; x < mapState.MapSize.x; x++)
            {
                char symbol = objectTypeSymbolPairs.FirstOrDefault(pair => pair.Value == mapState.GetGridObject(x, y)).Key;
                debugMapState += symbol;
            }
        }

        Debug.Log(debugMapState);
    }
#endif

    public bool IsLevelSolved()
    {
        for (int x = 0; x < mapState.MapSize.x; x++)
        {
            for (int y = 0; y < mapState.MapSize.y; y++)
            {
                GridObjectType gridObject = mapState.GetGridObject(x, y);
                if (gridObject == GridObjectType.Box)
                    return false;
            }
        }

        return true;
    }

    private void FitCameraToMap()
    {
        int biggerSide = Mathf.Max(mapState.MapSize.x, mapState.MapSize.y);
        Camera.main.orthographicSize = biggerSide / 2.0f;
        Camera.main.transform.position = new Vector3(mapState.Tilemap.size.x / 2.0f, mapState.Tilemap.size.y / 2.0f, Camera.main.transform.position.z);

    }

    private void MovePlayer(int2 moveTo)
    {
        GridObjectType destinationState = mapState.GetGridObject(moveTo.x, moveTo.y) == GridObjectType.Mark ? GridObjectType.PlayerOnMark : GridObjectType.Player;
        mapState.SetGridObject(destinationState, moveTo.x, moveTo.y);

        GridObjectType underPlayer = mapState.GetGridObject(mapState.PlayerPos.x, mapState.PlayerPos.y) == GridObjectType.PlayerOnMark ? GridObjectType.Mark : GridObjectType.Ground;
        mapState.SetGridObject(underPlayer, mapState.PlayerPos.x, mapState.PlayerPos.y);

        mapState.PlayerPos = moveTo;
    }

    private bool MoveBox(int2 boxPos, int2 moveTo)
    {
        if (!(mapState.GetGridObject(moveTo.x, moveTo.y) == GridObjectType.Mark || mapState.GetGridObject(moveTo.x, moveTo.y) == GridObjectType.Ground))
            return false;

        GridObjectType destinationState = mapState.GetGridObject(moveTo.x, moveTo.y) == GridObjectType.Mark ? GridObjectType.BoxOnMark : GridObjectType.Box;
        mapState.SetGridObject(destinationState, moveTo.x, moveTo.y);

        GridObjectType underBox = mapState.GetGridObject(boxPos.x, boxPos.y) == GridObjectType.BoxOnMark ? GridObjectType.Mark : GridObjectType.Ground;
        mapState.SetGridObject(underBox, boxPos.x, boxPos.y); 

        return true;
    }
}
