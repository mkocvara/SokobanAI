using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public static class SerializableExtensions
{
    public static SerializableGameRule[] ToSerializable(this List<GameRule> rules)
    {
        return rules.Select(rule => new SerializableGameRule(rule)).ToArray();
    }
}