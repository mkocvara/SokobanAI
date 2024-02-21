using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameRule
{
    public enum ActionType
    { 
        // TODO dummies
        MoveToBox,
        PushBox,
        MoveIntoWall
    }

    public ActionType Action;
    public int Reward;
}
