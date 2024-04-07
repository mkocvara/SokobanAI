using UnityEngine;

public class ToolbarFit : MonoBehaviour
{
    public GameObject Left, Right;
    public float padLeft = 0, padRight = 0;


    private RectTransform leftRt, rightRt, thisRect;

    void Start()
    {
        leftRt = Left.GetComponent<RectTransform>();
        rightRt = Right.GetComponent<RectTransform>();
        thisRect = GetComponent<RectTransform>();
    }

    void Update()
    {
        float offsetLeft = leftRt.offsetMin.x + leftRt.rect.width + padLeft;
        float offsetRight = rightRt.offsetMax.x - rightRt.rect.width - padRight;
        //float right = gameObject.GetComponentInParent<RectTransform>().rect.width - ((2 * btnRightRt.rect.position.x) - btnRightRt.rect.width);

        thisRect.offsetMin = new Vector2(offsetLeft, thisRect.offsetMin.y);
        thisRect.offsetMax = new Vector2(offsetRight, thisRect.offsetMax.y);
    }
}
