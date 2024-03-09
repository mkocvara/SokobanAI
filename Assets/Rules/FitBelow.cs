using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FitBelow : MonoBehaviour
{
    public GameObject ObjectToFitBelow;

    RectTransform thisObjectRt, topObjectRt;

    private bool fitSuccessful = false;
    private float cachedOffset = 0;

    void Start()
    {
        if (ObjectToFitBelow == null)
        {
            Debug.LogError("FitBelow.Start(): Object to fit under not assigned.");
            return;
        }
       
        thisObjectRt = GetComponent<RectTransform>();
        topObjectRt = ObjectToFitBelow.GetComponent<RectTransform>();
        fitSuccessful = Fit();
    }

    // Update is called once per frame
    void Update()
    {
        if (!fitSuccessful)
            return;

        if (topObjectRt.rect.height != cachedOffset)
            fitSuccessful = Fit();
    }

    private bool Fit()
    {
        if (thisObjectRt == null || topObjectRt == null)
            return false;

        cachedOffset = topObjectRt.rect.height;
        thisObjectRt.offsetMax = new Vector2(thisObjectRt.offsetMax.x, -cachedOffset);
        return true;
    }
}
