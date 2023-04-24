using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GesturePlayback : MonoBehaviour
{
    private GestureDetect gestureDetect;
    //gestureList needs to be list of Gesture objects?
    public Dictionary<string, Gesture> gestureDict;
    public List<Gesture> gestureList;
    // Not using Animator for now..
    public GameObject handModel;

    private int currentGestureIndex = 0;
    private float currentGestureTime = 0.0f;
    private float gestureDuration = 2f;
    private bool isPlaying = false;

    // Need another model for left and right hand? or just flip model?

    private void Start()
    {        
        gestureDict = GestureDetect.gestures;
        // fill gesture list with gestures from dict
        foreach(KeyValuePair<string, Gesture> kvp in gestureDict)
        {
            gestureList.Add(kvp.Value);
        }
    }

    private void Update()
    {
        if (isPlaying)
        {
            PlayGesture();
        }
    }

    public void StartPlayback()
    {
        isPlaying = true;
    }

    public void StopPlayback()
    {
        isPlaying=false;
        currentGestureIndex = 0;
        currentGestureTime = 0.0f;
        //reset position
    }

    public void PlayGesture()
    {
        
        if(currentGestureIndex >= gestureList.Count)
        {
            StopPlayback();
            return;
        }
        // commented out errors, seems disparaty between Gesture object and List<string, Gesture> gestureList
        Gesture currentGesture = gestureList[currentGestureIndex];
        currentGestureTime += Time.deltaTime;

        while (currentGestureTime > gestureDuration && currentGestureIndex < gestureList.Count - 1)
        {
            currentGestureTime -= gestureDuration;
            currentGestureIndex++;
            //currentGesture = gestureList[currentGestureIndex];
        }

        float t = currentGestureTime / gestureDuration;

        // Update the position and rotation of the hand model based on the finger data in the current gesture
        for (int i = 0; i < currentGesture.fingerDatas.Count; i++)
        {
            Transform fingerTransform = handModel.transform.Find(currentGesture.name);

            // REQUIRE REWORK OF FINGERDATAs TO INCLUDE START AND END POSITIONS?
            //fingerTransform.position = Vector3.Lerp(currentGesture.fingerDatas[i].startPosition, currentGesture.fingerDatas[i].endPosition, t);
            //fingerTransform.rotation = Quaternion.Slerp(currentGesture.fingerDatas[i].startRotation, currentGesture.fingerDatas[i].endRotation, t);
        }
        
    }
}
