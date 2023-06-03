using System.Collections;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit.Experimental.UI;
using UnityEngine;

public class MRTKTest : MonoBehaviour
{
    public TouchScreenKeyboard keyboard;
    public MixedRealityKeyboard mrtkKeyboard;
    // Start is called before the first frame update
    void Start()
    {
        keyboard = TouchScreenKeyboard.Open("Test", TouchScreenKeyboardType.Default, false, false, false, false);

    }

    // Update is called once per frame
    void Update()
    {
        if (keyboard != null)
        {
            Debug.Log(keyboard.text);
        }
        else
        {
            Debug.Log("Shits fucked");
        }
    }
}
