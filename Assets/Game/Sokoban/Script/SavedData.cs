using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SavedData
{
    public bool[] LevelsSolved;
    
    public SavedData()
    {
    }
    
    public SavedData(List<Level> levels)
    {
        LevelsSolved = new bool[levels.Count];
        for (int i = 0; i < levels.Count; i++)
        {
            LevelsSolved[i] = levels[i].IsSolved;
        }
    }
}
