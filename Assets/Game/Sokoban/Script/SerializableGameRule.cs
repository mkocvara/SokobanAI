using System;
using System.Collections.Generic;
using System.Linq;

[Serializable]
public struct SerializableGameRule
{
    public GameRule.ActionType Action;
    public int Reward;

    public SerializableGameRule(GameRule.ActionType action, int reward)
    {
        Action = action;
        Reward = reward;
    }

    public SerializableGameRule(GameRule rule)
    {
        Action = rule.Action;
        Reward = rule.Reward;
    }
}