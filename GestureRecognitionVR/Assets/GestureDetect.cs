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
using Newtonsoft.Json.Linq;
using static GestureDetect;

[System.Serializable]
public struct Gesture
{
    public string name;

    // fingerData represents multiple frames of SerializedBoneData for every bone 
    public List<Dictionary<string, SerializedBoneData>> fingerData;
    public UnityEvent onRecognized;

    public Gesture(string gestureName, List<Dictionary<string, SerializedBoneData>> fingerData, UnityAction func)
    {
        this.name = gestureName;
        this.fingerData = fingerData;

        onRecognized = new UnityEvent();
        onRecognized.AddListener(func);
    }

    public override bool Equals(object obj)
    {
        Vector3 test1 = Vector3.zero;
        Vector3 test2 = Vector3.zero;

        float diff = (test1 - test2).magnitude;
        //if current fingerdata = saved finger data, return.
        //iterate through fingerdata,

        //grab tip of thumb, find distance vector - vector.magnitude
        return base.Equals(obj);
    }
}

public class GestureDetect : MonoBehaviour
{

    // Hand Model Menu
    public GameObject handModel;
    public Microsoft.MixedReality.Toolkit.UI.Interactable nextButton;
    public Microsoft.MixedReality.Toolkit.UI.Interactable prevButton;
    private int currentGestureIndex = 0;
    public GesturePlayback gesturePlayback;

    // Set detectionThreshold. Smaller threshold = more precise hand detection. Set to 0.5.
    [SerializeField] private float detectionThreshold = 0.5f;

    // Hands to record
    [SerializeField] private OVRSkeleton[] hands;

    //Create List for Gestures
    public Dictionary<string, Gesture> gestures;

    // Record new gestures
    [Header("Recording")][SerializeField] private OVRSkeleton handToRecord;
    // NOTE: fingerBones is currently including all 24 bones in the hand
    private List<OVRBone> fingerBones = new List<OVRBone>();
    private float recordingTime = 0.01f; //set recording time default to 0.01 second (one frame, user should be able to change this)

    //Keep track of which Gesture was most recently recognized
    private Gesture? currentGesture;
    private Gesture? previousGesture;

    //Create cube object and renderer to change color when G1 is recognised (G1Routine). 
    [SerializeField] public GameObject cube;
    public Renderer cubeRenderer;
    public Color newColour;
    public Color oldColour;

    //Create second cube, which will be transformed to sphere when G2 is recognised (G2Routine).
    [SerializeField] public GameObject cube2;
    public GameObject sphere;

    //Create Dictionary to store Gestures
    Dictionary<string, UnityAction> gestureNames;
    public GameObject gestureNamerPrefab;
    public GameObject gestureNamerPosition;

    // Start is called before the first frame update
    void Start()
    {
        // Initialize the gestures dictionary with default gestures
        gestures = new Dictionary<string, Gesture>();

        // Save the gestures dictionary to JSON
        GesturesToJSON();
        //Read any previously saved Gestures from existing json data
        readGesturesFromJSON();

        //Set 3 default gestures at startup 
        gestureNames = new Dictionary<string, UnityAction>()
        {
            { "Gesture 1", G1 },
            { "Gesture 2", G2 },
            { "Gesture 3", G3 }
        };

        //For each Gesture in Dictionary, create cube button on table for recording that Gesture.
        Vector3 currentPos = gestureNamerPosition.transform.position;
        foreach (KeyValuePair<string, UnityAction> keyValuePair in gestureNames)
        {
            GameObject buttonCube = Instantiate(gestureNamerPrefab);
            GestureName gn = buttonCube.GetComponent<GestureName>();
            gn.gestName = keyValuePair.Key;
            gn.gestureDetection = this;
            buttonCube.transform.position = currentPos;

            currentPos.x += 0.2f;
        }

        //Add listeners to the Next and Previous MRTK buttons
        nextButton.OnClick.AddListener(NextGesture);
        prevButton.OnClick.AddListener(PrevGesture);

    }

    private void NextGesture()
    {
        // currentGestureIndex is used to cycle through recorded gestures
        currentGestureIndex++;

        // If the currentGestureIndex exceeds the range, wrap around to the first gesture
        if (currentGestureIndex >= gestures.Count)
        {
            currentGestureIndex = 0;
        }

        //Debug.Log($"Gesture Index: {currentGestureIndex}");

        // Check if the currentGestureIndex is within the valid range
        if (currentGestureIndex >= 0 && currentGestureIndex < gestures.Count)
        {
            Gesture currentGesture = gestures.Values.ElementAt(currentGestureIndex);
            string gestureName = currentGesture.name;
            //Debug.Log($"Gesture Name: {gestureName}");
            gesturePlayback.PlayGesture(gestureName);
        }
        else
        {
            Debug.LogError("Invalid gesture index.");
        }
    }


    private void PrevGesture()
    {
        // If there are no gestures recorded, return or handle the case appropriately
        if (gestures.Count == 0)
        {
            Debug.LogWarning("No gestures recorded.");
            return;
        }

        // currentGestureIndex is used to cycle through recorded gestures
        currentGestureIndex--;

        // If user goes back past the first gesture, go to the end of the gesture list
        if (currentGestureIndex < 0)
        {
            currentGestureIndex = gestures.Count - 1;
        }

        // Get current gesture name, playback the gesture
        Gesture currentGesture = gestures.Values.ElementAt(currentGestureIndex);
        string gestureName = currentGesture.name;
        gesturePlayback.PlayGesture(gestureName);
    }


    // Update is called once per frame
    void Update()
    {
        // Search for user Hands
        hands = FindObjectsOfType<OVRSkeleton>();
        findHandtoRecord();

        // Check for Recognition (returns recognized Gesture if hand is in correct position)
        currentGesture = Recognize();
        bool hasRecognized = currentGesture.HasValue;

        // Check if gesture is recognizable and new, log recognized gesture
        if (hasRecognized && (!previousGesture.HasValue || !currentGesture.Value.Equals(previousGesture.Value)))
        {
            Debug.Log("Gesture Recognized: " + currentGesture.Value.name);
            previousGesture = currentGesture;

            // Invoke onRecognized event if currentGesture is not null and onRecognized is not null
            if (currentGesture != null && currentGesture.Value.onRecognized != null && currentGesture.Value.onRecognized.GetPersistentEventCount() > 0)
            {
                currentGesture.Value.onRecognized.Invoke();
            }
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

            if (handToRecord != null && handToRecord.Bones != null && handToRecord.Bones.Count > 0)
            {
                // Need every bone in hand to determine local position of fingers
                fingerBones = handToRecord.Bones.ToList();
            }
            else
            {
                //Debug.Log("No hand detected");
            }
        }
    }


    public void Save(string name)
    {
        StartCoroutine(SaveGesture(name, recordingTime));
    }

    // Save coroutine for motion gestures
    public IEnumerator SaveGesture(string name, float recordingTime)
    {
        Gesture g = new Gesture();
        g.name = name;
        g.fingerData = new List<Dictionary<string, SerializedBoneData>>();

        float startTime = Time.time;

        while (Time.time - startTime < recordingTime)
        {
            // Save each individual finger bone in fingerData
            Dictionary<string, SerializedBoneData> frameData = new Dictionary<string, SerializedBoneData>();

            foreach (OVRBone bone in fingerBones)
            {
                string boneName = bone.Id.ToString();

                SerializedBoneData boneData = new SerializedBoneData();
                boneData.boneName = bone.Transform.name;
                boneData.position = bone.Transform.localPosition;
                boneData.rotation = bone.Transform.localRotation;

                frameData[boneName] = boneData;
            }

            // Add the frame data to the fingerData list
            g.fingerData.Add(frameData);

            yield return null;
        }

        g.onRecognized = new UnityEvent();
        g.onRecognized.AddListener(gestureNames[g.name]);

        // Add gesture to Gesture List
        gestures[name] = g;

        if (recordingTime > 0.01f)
        {
            Debug.Log("Saved Motion Gesture: " + name);
        }
        else
        {
            Debug.Log("Saved Static Gesture: " + name);
        }

        GesturesToJSON();
    }


    public class SerializedBoneData
    {
        public string boneName;
        public Vector3 position;
        public Quaternion rotation;
    }


    //Save gestures in Gesture List as JSON data
    public void GesturesToJSON()
    {
        if (gestures.Count == 0)
        {
            //Debug.Log("gestures is empty");
            return;
        }

        // Serialize the dictionary of serialized gestures to JSON
        string json = JsonConvert.SerializeObject(gestures, Formatting.Indented, new JsonSerializerSettings()
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        });

        // check serialization...
        //Debug.Log("Number of serialized gestures: " + gestures.Count);

        string directory = Application.persistentDataPath + "/GestureRecognitionVR/";
        string saveFile = directory + "savedGestures.json";

        // check json was saved
        // Debug.Log("Serialized gestures JSON: " + json);

        //If json directory does not exist, create it
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        //If a previous saveFile exists, delete
        if (File.Exists(saveFile))
        {
            File.Delete(saveFile);
        }

        //Save json data to new file
        File.WriteAllText(saveFile, json);
    }



    // Read data from JSON file, deserialize data into Gestures, populate Gesture list
    public void readGesturesFromJSON()
    {
        string directory = Application.persistentDataPath + "/GestureRecognitionVR/";
        string saveFile = directory + "savedGestures.json";
        //Debug.Log("JSON Path: " + saveFile);

        if (File.Exists(saveFile))
        {
            string contents = File.ReadAllText(saveFile);
            gestures = JsonConvert.DeserializeObject<Dictionary<string, Gesture>>(contents);
        }
        else
        {
            gestures = new Dictionary<string, Gesture>();
        }
    }

    /*
    private void OnValidate()
    {
        // update JSON if any changes to the gesture list have been made
        if (gestures.list.Count > 0)
        {
            GesturesToJSON();
        }
    }
    */


    //Starts the G1Routine when "Gesture 1" is recognised
    public void G1()
    {
        StartCoroutine(G1Routine());
    }

    //When "Gesture 1" is recognised...
    public IEnumerator G1Routine()
    {
        //If current gesture has name "Gesture 1", change cube color to green for 2 seconds, then back to red.
        cubeRenderer.material.color = newColour;

        yield return new WaitForSeconds(2);

        cubeRenderer.material.color = oldColour;
    }

    //Starts the G2Routine when "Gesture 2" is recognised 
    public void G2()
    {
        StartCoroutine(G2Routine());
    }

    public IEnumerator G2Routine()
    {
        //If current gesture has name "Gesture 2", change cube to a sphere. After 2 seconds, it will change back.
        cube2.SetActive(false);
        sphere.SetActive(true);

        yield return new WaitForSeconds(2);

        cube2.SetActive(true);
        sphere.SetActive(false);
    }

    //Starts the G3Routine when "Gesture 3" is recognised
    public void G3()
    {
        StartCoroutine(G3Routine());
    }

    public IEnumerator G3Routine()
    {
        //if current gesture is "Gesture 3", change cube color and change cube to sphere for 2 seconds
        cubeRenderer.material.color = newColour;
        cube2.SetActive(false);
        sphere.SetActive(true);

        yield return new WaitForSeconds(2);

        cubeRenderer.material.color = oldColour;
        cube2.SetActive(true);
        sphere.SetActive(false);
    }

    // Check if current hand gesture is a recorded gesture TODO: make work
    /*
      Start by iterating over each gesture, both motion and static gestures.
      For each gesture, compare the first frame of the recorded motion data with the current frame being played.
      If there is a match, store a counter or a progression value to represent your progression through that specific gesture.
      If no motion gesture matches, proceed to check static gestures.
      For subsequent frames, compare the next frame of the recorded motion data with the current frame being played.
      If there is a match, increment the counter or progression value for the corresponding motion gesture.       
      If the current or next frame doesn't match the recorded motion data, reset the counter or progression value and continue checking for other gestures.
   */

    private Gesture? Recognize()
    {
        foreach (KeyValuePair<string, Gesture> kvp in gestures)
        {
            Gesture gesture = kvp.Value;

            // Check if it's a motion gesture
            if (gesture.fingerData.Count > 1)
            {
                if (MatchMotionGesture(gesture))
                {
                    return gesture;
                }
            }
            else if (gesture.fingerData.Count == 1)
            {
                if (MatchStaticGesture(gesture))
                {
                    return gesture;
                }
            }
        }

        // No gesture matched
        return null;
    }

    // Checks to see if current gesture is matching a saved motion gesture
    private bool MatchMotionGesture(Gesture gesture)
    {
        // Iterate over each frame of the motion gesture
        foreach (Dictionary<string, SerializedBoneData> motionFrame in gesture.fingerData)
        {
            bool match = true;

            // Iterate over each finger bone in the frame data
            foreach (KeyValuePair<string, SerializedBoneData> kvp in motionFrame)
            {
                string boneName = kvp.Key;
                SerializedBoneData motionBoneData = kvp.Value;

                // Check if the bone name exists in the current frame data
                if (!gesture.fingerData[0].ContainsKey(boneName)) //why 0 here?
                {
                    match = false;
                    break;
                }

                SerializedBoneData frameBoneData = gesture.fingerData[0][boneName];

                // Compare the bone positions and rotations
                if (!CompareBoneData(frameBoneData, motionBoneData))
                {
                    match = false;
                    break;
                }
            }

            if (match)
            {
                return true;
            }
        }

        return false;
    }


    // Checks to see if the current gesture matches saved static gesture
    private bool MatchStaticGesture(Gesture gesture)
    {
        Dictionary<string, SerializedBoneData> gestureData = gesture.fingerData[0];

        foreach (KeyValuePair<string, SerializedBoneData> kvp in gestureData)
        {
            string boneName = kvp.Key;
            SerializedBoneData gestureBoneData = kvp.Value;

            // Check if the bone name exists in the frame data
            if (!gesture.fingerData[0].ContainsKey(boneName))
            {
                return false;
            }

            SerializedBoneData frameBoneData = gesture.fingerData[0][boneName];

            // Compare the bone positions and rotations
            if (!CompareBoneData(frameBoneData, gestureBoneData))
            {
                return false;
            }
        }

        return true;
    }

    // Compares the position and rotation values of two bones against detectionThreshold
    private bool CompareBoneData(SerializedBoneData boneData1, SerializedBoneData boneData2)
    {
        // Compare the bone positions
        if (Vector3.Distance(boneData1.position, boneData2.position) > detectionThreshold)
        {
            return false;
        }

        // Compare the bone rotations
        if (Quaternion.Angle(boneData1.rotation, boneData2.rotation) > detectionThreshold)
        {
            return false;
        }

        return true;
    }









}

//Inspector Record Button, no longer used.
/*#if UNITY_EDITOR
[CustomEditor(typeof(GestureDetect))]
public class GestureInspector : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GestureDetect gestureDetect = (GestureDetect)target;

        if (GUILayout.Button("Record a Gesture"))
        {
            gestureDetect.Save();
            gestureDetect.GesturesToJSON();
        }
    }
}
#endif*/