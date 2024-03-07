using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FitAtTop : MonoBehaviour
{
    public GameObject TopObjectToFitUnder;

    // Start is called before the first frame update
    void Start()
    {
        if (TopObjectToFitUnder == null)
        {
            TopObjectToFitUnder = transform.parent.GetChild(0).gameObject;
            if (TopObjectToFitUnder == gameObject || TopObjectToFitUnder == null)
            {
                Console.WriteLine("Error in FitAtTop: Can't find a valid object to fit under.");
                return;
            }
        }
       
        RectTransform thisObjectRt = GetComponent<RectTransform>();
        RectTransform topObjectRt = TopObjectToFitUnder.GetComponent<RectTransform>();
        thisObjectRt.offsetMax = new Vector2(thisObjectRt.offsetMax.x, -topObjectRt.rect.height);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
