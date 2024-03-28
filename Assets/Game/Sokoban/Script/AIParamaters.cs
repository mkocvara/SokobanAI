using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

[Serializable]
public class AIParameters
{
    [Serializable]
    public class ParametrisedGameRule
    {
        public GameRule.ActionType Action;
        public int Reward;

        public ParametrisedGameRule(GameRule.ActionType action, int reward)
        {
            Action = action;
            Reward = reward;
        }

        public ParametrisedGameRule(GameRule rule)
        {
            Action = rule.Action;
            Reward = rule.Reward;
        }
    }

    public int Level;
    public int NumGenerations;
    public ParametrisedGameRule[] Rules;

    public AIParameters(int level, int numGenerations, List<GameRule> rules)
    {
        Level = level;
        NumGenerations = numGenerations;
        Rules = rules.Select(rule => new ParametrisedGameRule(rule)).ToArray();        
    }
}