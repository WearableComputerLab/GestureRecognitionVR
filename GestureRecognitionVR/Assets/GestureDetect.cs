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

[System.Serializable]
public struct Gesture
{
    public string name;
    //public List<List<Vector3>> fingerData;
    // string is finger name and list is data over time
    public Dictionary<string, List<Vector3>> fingerData;

    // motionData = List of hand position/rotation over time
    public List<Vector3> motionData;
    public UnityEvent onRecognized;

    public Gesture(string gestureName, Dictionary<string, List<Vector3>> fingerData, List<Vector3> motionData, UnityAction func)
    {
        this.name = gestureName;
        this.fingerData = fingerData;
        this.motionData = motionData;

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

[System.Serializable]
public class SerializableList<T>
{
    public List<T> list;
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
    [Header("Recording")] [SerializeField] private OVRSkeleton handToRecord;
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
        gestures = new Dictionary<string, Gesture>();

        // Initialize the gestures dictionary with default gestures
        gestures = new Dictionary<string, Gesture>();

        /* Add default gestures to the dictionary
        gestures.Add("DefaultGesture1", new Gesture()
        {
            fingerData = new Dictionary<string, List<Vector3>>()
            {
            { "Index", new List<Vector3> { new Vector3(0.1f, 0.2f, 0.3f), new Vector3(0.4f, 0.5f, 0.6f), new Vector3(0.7f, 0.8f, 0.9f) } },
            { "Middle", new List<Vector3> { new Vector3(0.2f, 0.3f, 0.4f), new Vector3(0.5f, 0.6f, 0.7f), new Vector3(0.8f, 0.9f, 1.0f) } },
            { "Ring", new List<Vector3> { new Vector3(0.3f, 0.4f, 0.5f), new Vector3(0.6f, 0.7f, 0.8f), new Vector3(0.9f, 1.0f, 1.1f) } },
            { "Pinky", new List<Vector3> { new Vector3(0.4f, 0.5f, 0.6f), new Vector3(0.7f, 0.8f, 0.9f), new Vector3(1.0f, 1.1f, 1.2f) } },
            { "Thumb", new List<Vector3> { new Vector3(0.5f, 0.6f, 0.7f), new Vector3(0.8f, 0.9f, 1.0f), new Vector3(1.1f, 1.2f, 1.3f) } }
            },
            motionData = new List<Vector3>()
        });

        gestures.Add("DefaultGesture2", new Gesture()
        {
            fingerData = new Dictionary<string, List<Vector3>>()
            {
            { "Thumb", new List<Vector3> { new Vector3(0.1f, 0.2f, 0.3f), new Vector3(0.2f, 0.3f, 0.4f), new Vector3(0.3f, 0.4f, 0.5f) } },
            { "Index", new List<Vector3> { new Vector3(0.4f, 0.5f, 0.6f), new Vector3(0.5f, 0.6f, 0.7f), new Vector3(0.6f, 0.7f, 0.8f) } },
            { "Middle", new List<Vector3> { new Vector3(0.7f, 0.8f, 0.9f), new Vector3(0.8f, 0.9f, 1.0f), new Vector3(0.9f, 1.0f, 1.1f) } },
            { "Ring", new List<Vector3> { new Vector3(1.1f, 1.2f, 1.3f), new Vector3(1.2f, 1.3f, 1.4f), new Vector3(1.3f, 1.4f, 1.5f) } },
            { "Pinky", new List<Vector3> { new Vector3(1.4f, 1.5f, 1.6f), new Vector3(1.5f, 1.6f, 1.7f), new Vector3(1.6f, 1.7f, 1.8f) } }
            },
            motionData = new List<Vector3>()
        });
        */

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

        Debug.Log($"Gesture Index: {currentGestureIndex}");

        // Check if the currentGestureIndex is within the valid range
        if (currentGestureIndex >= 0 && currentGestureIndex < gestures.Count)
        {
            Gesture currentGesture = gestures.Values.ElementAt(currentGestureIndex);
            string gestureName = currentGesture.name;
            Debug.Log($"Gesture Name: {gestureName}");
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
                // NOTE: this gets all 24 bones in the hand: fingerBones = new List<OVRBone>(handToRecord.Bones);
                // This gets only bones in fingers
                fingerBones = new List<OVRBone>
                {
                    handToRecord.Bones[(int)OVRSkeleton.BoneId.Hand_Index1],
                    handToRecord.Bones[(int)OVRSkeleton.BoneId.Hand_Index2],
                    handToRecord.Bones[(int)OVRSkeleton.BoneId.Hand_Index3],
                    handToRecord.Bones[(int)OVRSkeleton.BoneId.Hand_Middle1],
                    handToRecord.Bones[(int)OVRSkeleton.BoneId.Hand_Middle2],
                    handToRecord.Bones[(int)OVRSkeleton.BoneId.Hand_Middle3],
                    handToRecord.Bones[(int)OVRSkeleton.BoneId.Hand_Pinky0],
                    handToRecord.Bones[(int)OVRSkeleton.BoneId.Hand_Pinky1],
                    handToRecord.Bones[(int)OVRSkeleton.BoneId.Hand_Pinky2],
                    handToRecord.Bones[(int)OVRSkeleton.BoneId.Hand_Ring1],
                    handToRecord.Bones[(int)OVRSkeleton.BoneId.Hand_Ring2],
                    handToRecord.Bones[(int)OVRSkeleton.BoneId.Hand_Ring3],
                    handToRecord.Bones[(int)OVRSkeleton.BoneId.Hand_Thumb2],
                    handToRecord.Bones[(int)OVRSkeleton.BoneId.Hand_Thumb3]
                };
            }
            else
            {
                Debug.Log("No hand detected");
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
        g.fingerData = new Dictionary<string, List<Vector3>>();
        g.motionData = new List<Vector3>();

        float startTime = Time.time;

        while (Time.time - startTime < recordingTime)
        {
            // Save each individual finger bone in fingerData
            foreach (OVRBone bone in fingerBones)
            {
                // Get the finger name based on the bone ID
                string fingerName = GetFingerName(bone.Id); 

                if (!g.fingerData.ContainsKey(fingerName))
                {
                    // Create a new list for the finger if it doesn't exist
                    g.fingerData[fingerName] = new List<Vector3>(); 
                }

                // Add the position to the list of the corresponding finger
                g.fingerData[fingerName].Add(handToRecord.transform.InverseTransformPoint(bone.Transform.position));
            }

            // Record hand motion data if it's a motion gesture
            if (recordingTime > 0.01f)
            {
                g.motionData.Add(handToRecord.transform.InverseTransformPoint(handToRecord.transform.position));
            }

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
    }

    private string GetFingerName(OVRSkeleton.BoneId boneId)
    {
        Dictionary<OVRSkeleton.BoneId, string> boneToFingerMap = new Dictionary<OVRSkeleton.BoneId, string>()
    {
        
        { OVRSkeleton.BoneId.Hand_Index1, "Index" },
        { OVRSkeleton.BoneId.Hand_Index2, "Index" },
        { OVRSkeleton.BoneId.Hand_Index3, "Index" },
        { OVRSkeleton.BoneId.Hand_Middle1, "Middle" },
        { OVRSkeleton.BoneId.Hand_Middle2, "Middle" },
        { OVRSkeleton.BoneId.Hand_Middle3, "Middle" },
        { OVRSkeleton.BoneId.Hand_Pinky1, "Pinky" },
        { OVRSkeleton.BoneId.Hand_Pinky2, "Pinky" },
        { OVRSkeleton.BoneId.Hand_Pinky3, "Pinky" },
        { OVRSkeleton.BoneId.Hand_Ring1, "Ring" },
        { OVRSkeleton.BoneId.Hand_Ring2, "Ring" },
        { OVRSkeleton.BoneId.Hand_Ring3, "Ring" },
        { OVRSkeleton.BoneId.Hand_Thumb2, "Thumb" },
        { OVRSkeleton.BoneId.Hand_Thumb3, "Thumb" }
    };

        if (boneToFingerMap.ContainsKey(boneId))
        {
            return boneToFingerMap[boneId];
        }

        return "Unknown";
    }

    // Represents a serialized version of a gesture, used for JSON serialization/deserialization.
    // Contains the necessary properties to store and retrieve gesture data in a serialized format.
    public class SerializedGesture
    {
        public string name;
        public List<List<SerializedFingerData>> fingerData;
        public List<Vector3> motionData;
    }

    // Represents the finger data of a gesture, used for JSON serialization/deserialization.
    // Contains the necessary properties to store and retrieve finger data in a serialized format.
    public class SerializedFingerData
    {
        public string fingerName;
        public List<Vector3> positions;
    }


    //Save gestures in Gesture List as JSON data
       
    public void GesturesToJSON()
    {
        if (gestures.Count == 0)
        {
            Debug.Log("gestures is empty");
            return;
        }

        // Create a new dictionary to store the serialized gestures
        Dictionary<string, SerializedGesture> serializedGestures = new Dictionary<string, SerializedGesture>();

        // Iterate over each gesture in the gestures dictionary
        foreach (var kvp in gestures)
        {
            // Create a new serialized gesture object
            SerializedGesture serializedGesture = new SerializedGesture();

            // Set the gesture name
            serializedGesture.name = kvp.Key;

            // Check if finger data is null or empty
            if (kvp.Value.fingerData == null || kvp.Value.fingerData.Count == 0)
            {
                Debug.Log("Finger data is null or empty for gesture: " + kvp.Key);
                continue;
            }

            // Create a list to store the frames of finger data
            List<List<SerializedFingerData>> fingerDataFrames = new List<List<SerializedFingerData>>();

            // Iterate over each frame of finger data in the gesture's finger data
            foreach (var frameData in kvp.Value.fingerData)
            {
                // Create a list to store the finger data for each frame
                List<SerializedFingerData> fingerDataList = new List<SerializedFingerData>();

                // Iterate over each finger position in the frame's finger data
                foreach (var position in frameData.Value)
                {
                    // Create a new FingerData object with the finger name and position list
                    SerializedFingerData finger = new SerializedFingerData()
                    {
                        fingerName = frameData.Key,
                        positions = new List<Vector3>() { position }
                    };

                    // Add the finger data to the list for the current frame
                    fingerDataList.Add(finger);
                }

                // Add the finger data list for the current frame to the frames list
                fingerDataFrames.Add(fingerDataList);
            }

            // Set the finger data frames in the serialized gesture
            serializedGesture.fingerData = fingerDataFrames;

            // Set the motion data in the serialized gesture
            serializedGesture.motionData = kvp.Value.motionData;

            // Add the serialized gesture to the dictionary
            serializedGestures.Add(kvp.Key, serializedGesture);
        }

        // Serialize the dictionary of serialized gestures to JSON
        string json = JsonConvert.SerializeObject(serializedGestures, Formatting.Indented, new JsonSerializerSettings()
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore // Ignore null motionData fields
        });

        // check serialization...
        Debug.Log("Number of serialized gestures: " + serializedGestures.Count);

        string directory = Application.persistentDataPath + "/GestureRecognitionVR/";
        string saveFile = directory + "savedGestures.json";

        // check json was saved
        Debug.Log("Serialized gestures JSON: " + json);

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

    public void readGesturesFromJSON()
    {
        string directory = Application.persistentDataPath + "/GestureRecognitionVR/";
        string saveFile = directory + "savedGestures.json";
        Debug.Log("JSON Path: " + saveFile);

        if (File.Exists(saveFile))
        {
            string contents = File.ReadAllText(saveFile);
            Debug.Log("JSON Contents: " + contents);

            var jsonGestures = JObject.Parse(contents);

            gestures = new Dictionary<string, Gesture>();

            foreach (var gesture in jsonGestures)
            {
                string gestureName = gesture.Key;
                var jsonGesture = gesture.Value;

                // Extract the finger data from the JSON
                var jsonFingerData = (JArray)jsonGesture["fingerData"];
                Dictionary<string, List<Vector3>> fingerData = new Dictionary<string, List<Vector3>>();

                foreach (var jsonFingerArray in jsonFingerData)
                {
                    var jsonFinger = (JObject)jsonFingerArray[0];
                    string fingerName = jsonFinger["fingerName"].ToString();
                    var jsonPositions = (JArray)jsonFinger["positions"];

                    List<Vector3> fingerVectorData = new List<Vector3>();

                    foreach (var jsonVector in jsonPositions)
                    {
                        float x = float.Parse(jsonVector["x"].ToString());
                        float y = float.Parse(jsonVector["y"].ToString());
                        float z = float.Parse(jsonVector["z"].ToString());

                        Vector3 vector = new Vector3(x, y, z);
                        fingerVectorData.Add(vector);
                    }

                    fingerData.Add(fingerName, fingerVectorData);
                }

                // Extract the motion data from the JSON
                var jsonMotionData = (JArray)jsonGesture["motionData"];
                List<Vector3> motionData = null;

                if (jsonMotionData != null && jsonMotionData.Count > 0)
                {
                    motionData = new List<Vector3>();

                    foreach (var jsonVector in jsonMotionData)
                    {
                        float x = float.Parse(jsonVector["x"].ToString());
                        float y = float.Parse(jsonVector["y"].ToString());
                        float z = float.Parse(jsonVector["z"].ToString());

                        Vector3 vector = new Vector3(x, y, z);
                        motionData.Add(vector);
                    }
                }


                // Create a new Gesture object with the extracted data and add it to the gestures dictionary
                gestures.Add(gestureName, new Gesture(gestureName, fingerData, motionData, null));
            }
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

    //Check if current hand gesture is a recorded gesture. NOTE: NOT TESTED
    // velocityWeight allows us to finetune how sensitive recognition is concerning speed of gesture performance, could use detectionThreshold instead?
    private float velocityWeight = 0.5f;
    Gesture? Recognize()
    {
        Gesture? currentGesture = null;
        float currentMin = Mathf.Infinity;

        foreach (KeyValuePair<string, Gesture> kvp in gestures)
        {
            float sumDistance = 0;
            bool discard = false;

            // Compare finger positions for each frame in fingerData
            for (int i = 0; i < kvp.Value.fingerData.Count; i++)
            {
                for (int j = 0; j < fingerBones.Count; j++)
                {
                    // Get the finger name for the current bone
                    string fingerName = GetFingerName(fingerBones[j].Id);

                    // Check if the finger name exists in the gesture's fingerData dictionary
                    if (!kvp.Value.fingerData.ContainsKey(fingerName))
                    {
                        // If the finger name is not present, discard the gesture
                        discard = true;
                        break;
                    }

                    // Get the current finger bone position
                    Vector3 currentData = handToRecord.transform.InverseTransformPoint(fingerBones[j].Transform.position);

                    // Calculate the distance between the current bone position and the corresponding position in the gesture's fingerData
                    float fingerDistance = 0f;
                    if (i < kvp.Value.fingerData[fingerName].Count)
                    {
                        fingerDistance = Mathf.Abs(Vector3.Distance(currentData, kvp.Value.fingerData[fingerName][i]));
                    }


                    // If the finger distance exceeds the detection threshold, discard the gesture
                    if (fingerDistance > detectionThreshold)
                    {
                        discard = true;
                        break;
                    }

                    // Accumulate the finger distances
                    sumDistance += fingerDistance;
                }

                if (discard)
                {
                    break;
                }
            }

            // If fingerData is not correct, skip checking motionData
            if (!discard)
            {
                // Create Lists to store Velocity and Direction of the motionData (as Vector3s)
                List<Vector3> velocities = new List<Vector3>();
                List<Vector3> directions = new List<Vector3>();

                // Check if motionData is null or empty before processing
                if (kvp.Value.motionData != null && kvp.Value.motionData.Count > 1)
                {
                    // Calculate velocity and direction of movement for each item in motionData list
                    for (int i = 0; i < kvp.Value.motionData.Count - 1; i++)
                    {
                        // Calculate the displacement vector between consecutive motionData points
                        Vector3 displacement = kvp.Value.motionData[i + 1] - kvp.Value.motionData[i];

                        // Calculate velocity by dividing displacement by time
                        Vector3 velocity = displacement / Time.deltaTime;
                        velocities.Add(velocity.normalized);

                        // Normalize displacement to store the direction vector representing the movement direction
                        directions.Add(displacement.normalized);
                    }

                    // Compare velocity and direction vectors for motionData
                    for (int i = 0; i < directions.Count; i++)
                    {
                        // Use Dot Product of vectors to compare velocity, and Vector Angles to compare direction
                        float dotProduct = Vector3.Dot(velocities[i], handToRecord.transform.forward);
                        float angle = Vector3.Angle(velocities[i], handToRecord.transform.forward);

                        // Get combined 'distance' between vectors, using velocityWeight to determine the importance of velocity in the gesture
                        float combinedDistance = angle + (dotProduct * velocityWeight);

                        // If the combined distance exceeds the detection threshold, discard the gesture
                        if (combinedDistance > detectionThreshold)
                        {
                            discard = true;
                            break;
                        }

                        // Accumulate the combined distances
                        sumDistance += combinedDistance;
                    }
                }
            }

            if (!discard && sumDistance < currentMin)
            {
                // Update the currentGesture if the sumDistance is smaller than the current minimum
                currentMin = sumDistance;
                currentGesture = kvp.Value;
            }
        }

        return currentGesture;
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