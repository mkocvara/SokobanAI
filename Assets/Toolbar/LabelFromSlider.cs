using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LabelFromSlider : MonoBehaviour
{
    private TextMeshProUGUI label;

    void Start()
    {
        label = GetComponent<TextMeshProUGUI>();
    }

    public void SetFromNumber(float value)
    {
        label.text = value.ToString();
    }
}
