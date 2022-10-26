using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ConsoleLogger : MonoBehaviour
{
    public  TextMeshProUGUI tmp;
    private void Awake()
    {
        Application.logMessageReceived += ApplicationOnlogMessageReceived;
    }

    private void ApplicationOnlogMessageReceived(string condition, string stacktrace, LogType type)
    {
        if (!condition.StartsWith("[OVRManager]") && !condition.StartsWith("[OculusXRFeature]"))
        {
            tmp.text += condition + "\n";
        }

        if (tmp.isTextOverflowing)
        {
            tmp.text = condition + "\n";
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
