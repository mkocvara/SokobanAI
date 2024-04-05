using System;
using System.Collections.Generic;
using System.Linq;

[Serializable]
public class AIParameters
{

    public int Level;
    public int NumGenerations;
    public SerializableGameRule[] Rules;

    public AIParameters(int level, int numGenerations, List<GameRule> rules)
    {
        Level = level;
        NumGenerations = numGenerations;
        Rules = rules.ToSerializable();
    }
}