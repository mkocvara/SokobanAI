using System;
using System.Collections.Generic;
using System.Linq;

[Serializable]
public class AIParameters
{

    public int Level;
    public int NumGenerations;
    public int ExplorationThreshold;
    public SerializableGameRule[] Rules;

    public AIParameters(int level, int numGenerations, int exploThreshold, List<GameRule> rules)
    {
        Level = level;
        NumGenerations = numGenerations;
        ExplorationThreshold = exploThreshold;
        Rules = rules.ToSerializable();
    }
}