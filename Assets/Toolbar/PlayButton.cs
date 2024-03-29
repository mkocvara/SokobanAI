using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayButton : MonoBehaviour
{
    public GameObject PlayingBar;

    private Button btnComponent;
    private TextMeshProUGUI btnText;

    private bool playing = false;

    private readonly char playSymbol = '\u25b6'; // play arrow
    private readonly char stopSymbol = '\u25a0'; // black square
    private readonly float playTextExtraLeftPadding = 3f;

    // Start is called before the first frame update
    void Start()
    {
        btnComponent = GetComponent<Button>();
        btnText = GetComponentInChildren<TextMeshProUGUI>();
        
        PlayingBar.SetActive(false);
    }

    public void Toggle()
    {
        if (playing) 
        {
            btnText.text = playSymbol.ToString();

            Vector4 margin = btnText.margin;
            margin.x += playTextExtraLeftPadding;
            btnText.margin = margin;

            PlayingBar.SetActive(false);
        }
        else
        {
            btnText.text = stopSymbol.ToString();

            Vector4 margin = btnText.margin;
            margin.x -= playTextExtraLeftPadding;
            btnText.margin = margin;

            PlayingBar.SetActive(true);
        }
        
        playing = !playing;
    }
}