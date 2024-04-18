using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActivateBackgroundFade : MonoBehaviour
{
    public GameObject GameObjectToBind;

    private void OnEnable()
    {
        GameObjectToBind.SetActive(true);
    }

    private void OnDisable()
    {
        GameObjectToBind.SetActive(false);
    }
}
