using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GesturePlayback : MonoBehaviour
{
    private GestureDetect gestureDetect;
    private Dictionary<string, Gesture> gestureList;
    private List<string> gestureNames;
    private int currentGestureIndex;

    // Add Animator Component to handModel 
    public GameObject handModel;
    // Use Animator?
    private Animator handAnimator;

    // Need another model for left and right hand? or just flip model?

    private void Start()
    {
        gestureDetect = GetComponent<GestureDetect>();
        handAnimator = handModel.GetComponent<Animator>();
        gestureNames = new List<string>(gestureList.Keys);
        gestureList = GestureDetect.gestures;
    }

    private void Update()
    {
        
    }

    public void StartPlayback()
    {

    }

    public void StopPlayback()
    {

    }

    private void PlayGesture(string gestureName)
    {

    }
}
