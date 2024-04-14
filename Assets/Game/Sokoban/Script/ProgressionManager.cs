using System;
using UnityEngine;

public class ProgressionManager : MonoBehaviour
{
    [Serializable]
    public struct LevelElements
    {
        public int LevelNumber;
        public GameObject[] GameObjectsToHide;
        public Default[] DefaultValues;

        public GameObject this[int index]
        {
            get
            {
                return GameObjectsToHide[index];
            }
            set
            {
                GameObjectsToHide[index] = value;
            }
        }
    }

    public enum DefaultVariables
    {
        GenerationsToRun,
        ExplorationThreshold
    }

    [Serializable]
    public class Default
    {
        public DefaultVariables Variable;
        public int Value;
    }

    public LevelElements[] Levels;

    private GameController gameController;
    private int lastLevel = 0;

    void Start()
    {
        gameController = FindObjectOfType<GameController>();
    }

    public void SetUpLevel(int level)
    {
        UpdateElementsForLevel(lastLevel, true);
        UpdateElementsForLevel(level, false);

        SetDefaultsForLevel(level);

        lastLevel = level;
    }

    private void UpdateElementsForLevel(int level, bool hidden)
    {
        foreach (LevelElements le in Levels)
        {
            if (le.LevelNumber == level)
            {
                foreach (GameObject go in le.GameObjectsToHide)
                    go.SetActive(hidden);
                return;
            }
        }
    }

    private void SetDefaultsForLevel(int level)
    {
        foreach (LevelElements le in Levels)
        {
            if (le.LevelNumber == level)
            {
                foreach(Default d in le.DefaultValues)
                {
                    switch(d.Variable)
                    {
                        case DefaultVariables.GenerationsToRun:
                            gameController.SetNumGenerations(d.Value);
                            break;

                        case DefaultVariables.ExplorationThreshold:
                            gameController.SetExplorationThreshold(d.Value);
                            break;

                        default:
                            break;
                    }
                }
                return;
            }
        }
    }
}
