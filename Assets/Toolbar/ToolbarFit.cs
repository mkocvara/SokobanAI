using UnityEngine;

public class ToolbarFit : MonoBehaviour
{
    public GameObject btnLeft, btnRight;

    private RectTransform btnLeftRt, btnRightRt, thisRect;

    void Start()
    {
        btnLeftRt = btnLeft.GetComponent<RectTransform>();
        btnRightRt = btnRight.GetComponent<RectTransform>();
        thisRect = GetComponent<RectTransform>();
    }

    void Update()
    {
        float left = (2 * btnLeftRt.offsetMin.x) + btnLeftRt.rect.width;
        float right = (2 * btnRightRt.offsetMax.x) - btnRightRt.rect.width;
        //float right = gameObject.GetComponentInParent<RectTransform>().rect.width - ((2 * btnRightRt.rect.position.x) - btnRightRt.rect.width);

        thisRect.offsetMin = new Vector2(left, thisRect.offsetMin.y);
        thisRect.offsetMax = new Vector2(right, thisRect.offsetMax.y);
    }
}
