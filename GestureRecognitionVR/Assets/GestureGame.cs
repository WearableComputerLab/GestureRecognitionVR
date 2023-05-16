using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.UI;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Input;
using System.Linq;

// This class contains all game (rock, paper, scissor) logic 
public class GestureGame : MonoBehaviour
{

    // Hand Model 
    public GameObject handModel;

    // Create List for Gestures to load from JSON
    public Dictionary<string, Gesture> gestures;

    // Hands to record
    [SerializeField] private OVRSkeleton[] hands;

    // Record new gestures
    [Header("Recording")][SerializeField] private OVRSkeleton handToRecord;
    private List<OVRBone> fingerBones = new List<OVRBone>();
   

    //Keep track of which Gesture was most recently recognized
    private Gesture? currentGesture;
    private Gesture? previousGesture;

    //Create Dictionary to store Gestures (use this for specific gesture actions)
    Dictionary<string, UnityAction> gestureNames;
    public GameObject gestureNamerPrefab;
    public GameObject gestureNamerPosition;

    // Start is called before the first frame update
    void Start()
    {
        //Read any previously saved Gestures from existing json data
        gestures = GlobalManager.Instance.GetGestures();

        //Set 3 default gestures at startup 
        gestureNames = new Dictionary<string, UnityAction>()
        {
            { "Gesture 1", G1 },
            { "Gesture 2", G2 },
            { "Gesture 3", G3 }
        };

    }

    public float UpdateFrequency = 0.05f; // 20 times per second (fine-tune along with frameTime in SaveGesture())
    private float lastUpdateTime;
    // Update is called once per frame
    void Update()
    {
        
        //Check for Recognition 20 times a second, same as captured data (returns recognised Gesture if hand is in correct position)
        //NOTE: possible for recognise() to miss start of gesture (fine-tune frequency)
        // if (Time.time > lastUpdateTime + UpdateFrequency)
        // {
        //     currentGesture = Recognize();
        //     lastUpdateTime = Time.time;
        // }

        currentGesture = GlobalManager.Instance.GestureDetect.Recognize();

        bool hasRecognized = currentGesture.HasValue;
        //Check if gesture is recognisable and new, log recognised gesture
        if (hasRecognized && (!previousGesture.HasValue || !currentGesture.Value.Equals(previousGesture.Value)))
        {
            Debug.Log("Gesture Recognized: " + currentGesture.Value.name);
            previousGesture = currentGesture;
            currentGesture.Value.onRecognized.Invoke();
        }
    }

    /// 
    /// Find a hand to record 
    /// Set finger bones for hand
    private void findHandtoRecord()
    {
        if (hands.Length > 0)
        {
            handToRecord = hands[0];
            fingerBones = new List<OVRBone>(handToRecord.Bones);
        }
    }

   
    //Starts the G1Routine when "Gesture 1" is recognised
    public void G1()
    {
        StartCoroutine(G1Routine());
    }

    //When "Gesture 1" is recognised...
    public IEnumerator G1Routine()
    {
        // G1 code here

        yield return new WaitForSeconds(2);

    }

    //Starts the G2Routine when "Gesture 2" is recognised 
    public void G2()
    {
        StartCoroutine(G2Routine());
    }

    public IEnumerator G2Routine()
    {
        // G2 code here

        yield return new WaitForSeconds(2);

    }

    //Starts the G3Routine when "Gesture 3" is recognised
    public void G3()
    {
        StartCoroutine(G3Routine());
    }

    public IEnumerator G3Routine()
    {
        // G3 code here

        yield return new WaitForSeconds(2);

    }

}
