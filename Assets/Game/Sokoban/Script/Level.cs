using System;
using System.IO;
using UnityEngine;

public class Level
{
    public static readonly string LevelDirectory = Application.dataPath + "/Game/Sokoban/Levels/";

    public int LevelNumber { get { return levelNumber; } }
    public string MapName { get { return mapName; } }
    public string MapString { get { return mapString; } }
    public string Instructions { get { return instructions; } }

    public bool IsCompleted { get; set; } = false;

    private readonly int levelNumber;
    private string mapName = "";
    private string mapString = "";
    private string instructions = "";

    public Level(int number)
    {
        levelNumber = number;
        LoadMapFromFile();
    }

    private void LoadMapFromFile()
    {
        if (!File.Exists(LevelDirectory + LevelNumber))
        {
            Console.WriteLine("Warning: Level file not found: " + LevelDirectory + LevelNumber);
            return;
        }

        StreamReader inputStream = new(LevelDirectory + LevelNumber);

        char readElement = 'x';

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
                    mapName += line;
                    break;
                case 'M':
                    mapString += (mapString.Length != 0 ? "\n" : "") + line; // add newline if not first line
                    break;
                case 'I':
                    instructions += (mapString.Length != 0 ? "\n" : "") + line; // add newline if not first line
                    break;
                default:
                    break;
            }
        }

        inputStream.Close();

#if DEBUG
        Console.WriteLine("Debug: MAP STRING:\n" + MapString);
#endif
    }
}
