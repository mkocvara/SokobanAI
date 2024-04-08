using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProgressionManager : MonoBehaviour
{
    [Header("Level 1")]
    public GameObject[] GameObjectsToHide1;

    [Header("Level 2")]
    public GameObject[] GameObjectsToHide2;

    [Header("Level 3")]
    public GameObject[] GameObjectsToHide3;

    [Header("Level 4")]
    public GameObject[] GameObjectsToHide4;

    [Header("Level 5")]
    public GameObject[] GameObjectsToHide5;

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
        switch (level)
        {
            case 1:
                foreach (GameObject go in GameObjectsToHide1)
                    go.SetActive(hidden);
                break;
            case 2:
                foreach (GameObject go in GameObjectsToHide2)
                    go.SetActive(hidden);
                break;
            case 3:
                foreach (GameObject go in GameObjectsToHide3)
                    go.SetActive(hidden);
                break;
            case 4:
                foreach (GameObject go in GameObjectsToHide4)
                    go.SetActive(hidden);
                break;
            case 5:
                foreach (GameObject go in GameObjectsToHide5)
                    go.SetActive(hidden);
                break;
        }
    }

    private void SetDefaultsForLevel(int level)
    {
        switch (level)
        {
            case 1:
                {
                    gameController.SetNumGenerations(gameController.DefaultGenerationsToRun);
                    gameController.SetExplorationThreshold(gameController.DefaultExplorationThreshold);
                    break;
                }

            case 2:
                {
                    gameController.SetExplorationThreshold(gameController.DefaultExplorationThreshold);
                    break;
                }

            case 3:
                {
                    break;
                }

            case 4:
                {
                    break;
                }

            case 5:
                { 
                    break;
                }
        }
    }
}
