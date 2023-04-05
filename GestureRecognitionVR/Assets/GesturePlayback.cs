using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GesturePlayback : MonoBehaviour
{
    private GestureDetect gestureDetect;
    private Dictionary<string, Gesture> gestureList;
    private int currentGestureIndex;
    public GameObject handModel;

    private void Start()
    {
        gestureDetect = GetComponent<GestureDetect>();
        gestureList = GestureDetect.gestures;
    }

    private void Update()
    {
        
    }
}
