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
using TMPro;

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

    // Set detectionThreshold. Smaller threshold = more precise hand detection. Set to 0.5. (Was [SerializeField])
    private float detectionThresholdPosition = 0.5f;
    private float detectionThresholdRotation = 10f;

    // Hands to record
    [SerializeField] private OVRSkeleton[] hands;

    //Create List for Gestures
    public Dictionary<string, Gesture> gestures;

    // Record new gestures
    [Header("Recording")][SerializeField] private OVRSkeleton handToRecord;
    // NOTE: fingerBones is currently including all 24 bones in the hand
    private List<OVRBone> fingerBones = new List<OVRBone>();

    // set recording time default to 0.01 second (one frame, user should be able to change this)
    private float recordingTime = 1f;
    // lastRecordingTime, isRecording, and delay are used to ensure a gesture isnt recognised as soon as it is recorded.
    private float lastRecordTime = 0f;
    private float delay = 2.0f; //delay of 2 seconds after recording before gesture can be recognized
    private bool isRecording = false;

    // Text on Table for important messages
    public TextMeshProUGUI userMessage;

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
        userMessage.text = "Welcome";
        // Initialize the gestures dictionary with default gestures
        gestures = new Dictionary<string, Gesture>();

        //Read any previously saved Gestures from existing json data
        readGesturesFromJSON();
        // Save the gestures dictionary to JSON
        GesturesToJSON();
        
        
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
        findHandToRecord();

        //Debug.Log(handToRecord.transform.position);

        // Check for Recognition (returns recognized Gesture if hand is in correct position)
        currentGesture = Recognize();
        bool hasRecognized = currentGesture.HasValue;

        // Check if gesture is recognizable and new, log recognized gesture
        if (hasRecognized && (!previousGesture.HasValue || !currentGesture.Value.Equals(previousGesture.Value)))
        {
            Debug.Log("Gesture Recognized: " + currentGesture.Value.name);
            userMessage.text = $"Recognized: {currentGesture.Value.name}";
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
    // Set finger bones for hand
    private void findHandToRecord()
    {
        if (hands.Length > 0)
        {
            // Hand Menu works when handToRecord is hands[0] (the GhostHand)
            // hands[2] = OVRRightHandPrefab
            handToRecord = hands[0];

            if (handToRecord != null && handToRecord.Bones != null && handToRecord.Bones.Count > 0)
            {
                // Need every bone in hand to determine local position of fingers
                fingerBones = new List<OVRBone>(handToRecord.Bones);
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
        isRecording = true;
        Gesture g = new Gesture();
        g.name = name;
        g.fingerData = new List<Dictionary<string, SerializedBoneData>>();
        float startTime = Time.time;
        int lastSecondDisplayed = Mathf.FloorToInt(startTime);

        // Find the OVRRightHandPrefab in the hands array (the hand we'll be recording)
        OVRSkeleton rightHand = null;
        foreach (OVRSkeleton hand in hands)
        {
            if (hand.transform.name == "OVRRightHandPrefab")
            {
                rightHand = hand;
                break;
            }
        }

        // If right hand cant be found, error
        if (rightHand == null)
        {
            Debug.LogError("Failed to find OVRRightHandPrefab in the hands array.");
            yield break;
        }

        while (Time.time - startTime < recordingTime)
        {
            // Timer countdown for time left to record
            int currentSecond = Mathf.FloorToInt(Time.time);
            if (currentSecond > lastSecondDisplayed)
            {
                float remainingTime = recordingTime - (Time.time - startTime);
                string sec = " seconds";
                if (remainingTime < 2) { sec = " second"; }
                Debug.Log("Recording: " + Mathf.FloorToInt(remainingTime).ToString() + sec);
                lastSecondDisplayed = currentSecond;
            }

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

            // Get the hand position and rotation data of hand being recorded
            Vector3 handPosition = rightHand.transform.position;
            Quaternion handRotation = rightHand.transform.rotation;

            // Set the handData with handPosition and handRotation for each frame
            SerializedBoneData handData = new SerializedBoneData();
            handData.boneName = "hand_R";
            handData.position = handPosition;
            handData.rotation = handRotation;
            frameData["HandPosition"] = handData;

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
            userMessage.text = $"Saved Motion Gesture: {name}";
        }
        else
        {
            Debug.Log("Saved Static Gesture: " + name);
            userMessage.text = $"Saved Static Gesture: {name}";
        }

        // set time when gesture was recorded, set isRecording to false
        lastRecordTime = Time.time;
        isRecording = false;

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
            //Debug.Log("File exists");
            string contents = File.ReadAllText(saveFile);
            gestures = JsonConvert.DeserializeObject<Dictionary<string, Gesture>>(contents);
        }
        else
        {
            //Debug.Log("Cant find json");
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
   */ // Possibly use motionThreshold variable to accept gesture as recognized easier? OR change detectionThreshold when detecting motion.
      // and what about whole hand world position?

    Gesture? Recognize()
    {
        Gesture? currentGesture = null;
        float currentMin = Mathf.Infinity;
        int motionCounter = 0;

        // Find the OVRRightHandPrefab in the hands array (the hand we'll be recognising)
        OVRSkeleton rightHand = null;
        foreach (OVRSkeleton hand in hands)
        {
            if (hand.transform.name == "OVRRightHandPrefab")
            {
                rightHand = hand;
                break;
            }
        }

        // Create a dictionary to store finger bones by their bone names (a snapshot of the current position of the user's hand)
        Dictionary<string, OVRBone> fingerBonesDict = new Dictionary<string, OVRBone>();

        // Populate the fingerBonesDict dictionary with all bones in the current hand
        foreach (OVRBone bone in fingerBones)
        {
            string boneName = bone.Id.ToString();
            fingerBonesDict[boneName] = bone;
        }

        // Check that gesture is not currently being recorded, and that one second (delay) has passed from when gesture was recorded.
        if (!isRecording && Time.time > lastRecordTime + delay)
        {
            // Going through each saved Gesture
            foreach (KeyValuePair<string, Gesture> kvp in gestures)
            {
                Gesture gesture = kvp.Value;

                float sumDistance = 0;
                bool discard = false;
                bool isMotionGesture = kvp.Value.fingerData.Count > 1;

                // Check if it's a motion gesture and reset the motion counter
                if (isMotionGesture)
                {
                    motionCounter = 0;
                }

                // Iterate over each frame of the gesture's fingerData
                foreach (var frameData in gesture.fingerData)
                {
                    // Compare the finger bone positions and rotations with the user's current hand
                    foreach (var boneEntry in frameData)
                    {
                        string boneName = boneEntry.Key;

                        // Ignore position of whole hand when looking through bones
                        if (boneName != "HandPosition")
                        {
                            // Check if the bone name exists in the current frame data and in the user's hand bone data
                            if (!frameData.ContainsKey(boneName) || !fingerBonesDict.ContainsKey(boneName))
                            {
                                //Debug.Log($"Bone: {boneName} not found in gesture or user's hand");
                                discard = true;
                                break;
                            }

                            // Get the position and rotation of the saved bone from the gesture data
                            SerializedBoneData gestureBoneData = boneEntry.Value;
                            Vector3 gestureBonePosition = gestureBoneData.position;
                            Quaternion gestureBoneRotation = gestureBoneData.rotation;

                            // Get the position and rotation of the corresponding bone from the user's hand
                            Vector3 currentBonePosition = fingerBonesDict[boneName].Transform.localPosition;
                            Quaternion currentBoneRotation = fingerBonesDict[boneName].Transform.localRotation;

                            // Compare the position and rotation of the bones using the CompareBoneData method
                            if (!CompareBoneData(gestureBonePosition, gestureBoneRotation, currentBonePosition, currentBoneRotation))
                            {
                                discard = true;
                                break;
                            }

                            // Calculate the distance between the current frame data and the user's current hand position
                            float positionDistance = Vector3.Distance(currentBonePosition, gestureBonePosition);
                            float rotationAngle = Quaternion.Angle(currentBoneRotation, gestureBoneRotation);

                            // Check if the position or rotation distance exceeds the detection threshold
                            if (positionDistance > detectionThresholdPosition || rotationAngle > detectionThresholdRotation)
                            {
                                discard = true;
                                break;
                            }

                            sumDistance += positionDistance + rotationAngle;
                        }

                    }

                    if (discard)
                    {
                        break;
                    }

                    // If it's a motion gesture, compare the frames and update the motion counter
                    if (isMotionGesture)
                    {
                        // Separate Thresholds for Recognizing Motion Gestures (1f and 20f)
                        detectionThresholdPosition = 1f;
                        detectionThresholdRotation = 35f;

                        // Threshold for how far into a motion gesture before it's recognized (90%)
                        int motionGestureThreshold = Mathf.CeilToInt(kvp.Value.fingerData.Count * 0.9f);

                        //Debug.Log($"Counter: {motionCounter}");
                        //Debug.Log($"Gesture length: {kvp.Value.fingerData.Count}");
                        // Debug.Log($"Threshold: {motionGestureThreshold}");

                        if (motionCounter >= motionGestureThreshold)
                        {
                            // Motion gesture has been matched completely
                            currentGesture = kvp.Value;
                            return currentGesture;
                        }
                        else if (motionCounter < kvp.Value.fingerData.Count)
                        {
                            Dictionary<string, SerializedBoneData> motionFrameData = kvp.Value.fingerData[motionCounter];
                            if (MatchMotionFrameData(frameData, motionFrameData, rightHand.transform.position, rightHand.transform.rotation))
                            {
                                motionCounter++;
                            }
                            else
                            {
                                motionCounter = 0;
                            }
                        }
                    }
                }

                if (!discard && sumDistance < currentMin && !isMotionGesture)
                {
                    currentMin = sumDistance;
                    currentGesture = kvp.Value;
                }
            }
        }

        return currentGesture;
    }

    // TODO: use HandPosition here to compare whole hand position across frames (introduce offset so position is independent of starting position)
    private bool MatchMotionFrameData(Dictionary<string, SerializedBoneData> frameData, Dictionary<string, SerializedBoneData> motionFrameData, Vector3 currentHandPosition, Quaternion currentHandRotation)
    {
        // Get the initial hand position from the motion frame data
        Vector3 initialHandPosition = motionFrameData["HandPosition"].position;

        // Calculate the translation offset
        Vector3 translationOffset = currentHandPosition - initialHandPosition;

        // Compare the hand positions using the adjusted positions
        SerializedBoneData motionHandData = motionFrameData["HandPosition"];
        Vector3 adjustedHandPosition = currentHandPosition - translationOffset;

        if (!CompareHandPosition(adjustedHandPosition, motionHandData.position, detectionThresholdPosition))
        {
            return false;
        }

        // Compare the hand rotations
        Quaternion motionHandRotation = motionHandData.rotation;

        if (!CompareRotationData(currentHandRotation, motionHandRotation, detectionThresholdRotation))
        {
            return false;
        }

        // Compare the bone positions and rotations
        foreach (KeyValuePair<string, SerializedBoneData> kvp in motionFrameData)
        {
            string boneName = kvp.Key;

            // Skip the hand position bone
            if (boneName == "HandPosition")
            {
                continue;
            }

            SerializedBoneData motionBoneData = kvp.Value;

            // Check if the bone name exists in the current frame data
            if (!frameData.ContainsKey(boneName))
            {
                return false;
            }

            SerializedBoneData frameBoneData = frameData[boneName];

            // Apply the translation offset to the motion bone position
            Vector3 adjustedBonePosition = frameBoneData.position - translationOffset;

            // Compare the bone positions and rotations using the adjusted positions
            if (!CompareBoneData(adjustedBonePosition, frameBoneData.rotation, motionBoneData.position, motionBoneData.rotation))
            {
                return false;
            }
        }

        return true;
    }

    // Compares the position and rotation values of two bones against detectionThreshold
    private bool CompareBoneData(Vector3 position1, Quaternion rotation1, Vector3 position2, Quaternion rotation2)
    {
        // Compare the bone positions
        if (Vector3.Distance(position1, position2) > detectionThresholdPosition)
        {
            //Debug.Log("Position don't match");
            return false;
        }

        // Compare the bone rotations
        if (Quaternion.Angle(rotation1, rotation2) > detectionThresholdRotation)
        {
            //Debug.Log("Rotation don't match");
            return false;
        }

        return true;
    }

    private bool CompareHandPosition(Vector3 handPosition1, Vector3 handPosition2, float detectionThreshold)
    {

        float positionDistance = Vector3.Distance(handPosition1, handPosition2);

        //Debug.Log($"Current Hand Position: {handPosition1}");
        //Debug.Log($"Saved Hand Position: {handPosition2}");
        //Debug.Log($"Distance: {positionDistance}");

        return positionDistance <= detectionThreshold;
    }

    private bool CompareRotationData(Quaternion rotation1, Quaternion rotation2, float detectionThreshold)
    {
        float rotationAngle = Quaternion.Angle(rotation1, rotation2);

        //Debug.Log($"Current Hand Rotation: {rotation1}");
        //Debug.Log($"Saved Hand Rotation: {rotation2}");
        //Debug.Log($"Angle: {rotationAngle}");

        return rotationAngle <= detectionThreshold;
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