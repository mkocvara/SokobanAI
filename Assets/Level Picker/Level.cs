using System;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Level : MonoBehaviour
{
    public Sprite CompletedImage, NotCompletedImage;
    public GameObject MapNumberTextObject, MapNameTextObject;

    public static readonly string LevelDirectory = Path.Combine(Application.streamingAssetsPath, "Levels");
    public string FileName { get { return GetFileNameFromLevelNumber(LevelNumber); } }
    public string LevelFilePath { get { return Path.Combine(LevelDirectory, FileName); } }

    public int LevelNumber { get; private set; }
    public string MapName { get; private set; }
    public string MapString { get; private set; }
    public string Instructions { get; private set; }
    public bool IsSolved { get; private set; } = false;
    

    private bool initialised = false;

    private Image buttonImage;
    private TextMeshProUGUI mapNumberTextMesh, mapNameTextMesh;
    private GameObject levelSolvedHint;

    private void Start()
    {
        // Hide until initialised
        if (!initialised) gameObject.SetActive(false);
    }

    public void Init(int number, GameObject levelSolvedHint, bool solved = false)
    {
        LevelNumber = number;
        this.levelSolvedHint = levelSolvedHint;
        buttonImage = GetComponent<Image>();
        SetSolved(solved);
        
        LoadMapFromFile();

        mapNumberTextMesh = MapNumberTextObject.GetComponent<TextMeshProUGUI>();
        mapNameTextMesh = MapNameTextObject.GetComponent<TextMeshProUGUI>();

        mapNumberTextMesh.text = LevelNumber.ToString();
        mapNameTextMesh.text = MapName;

        initialised = true;
        if (!gameObject.activeSelf) gameObject.SetActive(true);
    }

    public void SetSolved(bool solved)
    {
        IsSolved = solved;
        buttonImage.sprite = IsSolved ? CompletedImage : NotCompletedImage;
        levelSolvedHint.SetActive(IsSolved);
    }

    public static string GetFileNameFromLevelNumber(int levelNum)
    {
        return levelNum.ToString();
    }

    private void LoadMapFromFile()
    {
        if (!File.Exists(LevelFilePath))
        {
            Debug.LogWarning("Level.LoadMapFromFile(): Level file not found: \"" + LevelFilePath + "\"");
            return;
        }

        StreamReader inputStream = new(LevelFilePath);

        char readElement = 'x';

        MapName = MapString = Instructions = "";

        while (!inputStream.EndOfStream)
        {
            string line = inputStream.ReadLine();
            
            if (line.Length == 1)
            {
                readElement = line[0];
                continue;
            }

            switch (readElement)
            {
                case 'N':
                    MapName += line; // should be only one line
                    break;
                case 'M':
                    MapString += (MapString.Length != 0 ? "\n" : "") + line; // add newline if not first line
                    break;
                case 'I':
                    Instructions += (Instructions.Length != 0 ? "\n" : "") + line; // add newline if not first line
                    break;
                default:
                    break;
            }
        }

        inputStream.Close();

        //Debug.Log("Level.LoadMapFromFile(): MAP STRING:\n" + MapString);
    }
}
