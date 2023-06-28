using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Console Logger that displays all Debug.Log messages in the project
/// </summary>
public class ConsoleLogger : MonoBehaviour
{
    public TextMeshProUGUI tmp;

    /// <summary>
    /// Adds a listener to the Application.logMessageReceived event to display all Debug.Log messages in the project
    /// </summary>
    private void Awake()
    {
        Application.logMessageReceived += ApplicationOnlogMessageReceived;
    }

    /// <summary>
    /// Function that is called when a Debug.Log message is received to display errors, exceptions, and messages
    /// </summary>
    /// <param name="condition">The message being input</param>
    /// <param name="stacktrace">Any errors that may occur</param>
    /// <param name="type">Log type being input</param>
    private void ApplicationOnlogMessageReceived(string condition, string stacktrace, LogType type)
    {
        //If the message is not a warning or error, display it
        if (type != LogType.Warning)
        {
            //If the message is not an Oculus message, display it
            if (!condition.Contains("OVR") && !condition.StartsWith("["))
            {
                //Filter out specific messages
                if (!condition.StartsWith("[OVRManager]") && !condition.StartsWith("[OculusXRFeature]") &&
                    !condition.StartsWith("The current") &&
                    !condition.StartsWith("<color=\"#FFFF00\">[WARNING] </color>"))
                {
                    //Display the message, and if it is an exception, display the stacktrace on a new line
                    tmp.text += condition + "\n";
                    if (type == LogType.Exception)
                    {
                        tmp.text += stacktrace + "\n";
                    }
                }

                //If the text is overflowing, clear the text and display the message, and if it is an exception, display the stacktrace on a new line
                if (tmp.isTextOverflowing)
                {
                    tmp.text = condition + "\n";
                    if (type == LogType.Exception)
                    {
                        tmp.text += stacktrace + "\n";
                    }
                }
            }
        }
    }
}