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

[System.Serializable]
public struct Gesture
{
    public string name;
    public List<Vector3> fingerData;
    // motionData = List of hand position/rotation over time
    public List<Vector3> motionData;
    public UnityEvent onRecognized;

    public Gesture(UnityAction func)
    {
        name = "UNNAMED";
        fingerData = null;
        motionData = null;

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
    // Set detectionThreshold. Smaller threshold = more precise hand detection. Set to 0.5.
    [SerializeField] private float detectionThreshold = 0.5f;

    // Hands to record
    [SerializeField] private OVRSkeleton[] hands;

    //Create List for Gestures
    private Dictionary<string, Gesture> gestures;

    // Record new gestures
    [Header("Recording")] [SerializeField] private OVRSkeleton handToRecord;
    private List<OVRBone> fingerBones = new List<OVRBone>();

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
    }

    // Update is called once per frame
    void Update()
    {
        //Search for user Hands
        hands = FindObjectsOfType<OVRSkeleton>();
        findHandtoRecord();

        //Check for Recognition (returns recognised Gesture if hand is in correct position)
        currentGesture = Recognize();
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

    /// 
    /// Records a gesture when a Record Button is pressed within Scene
    /// ## WORK ON ADDING DATA TO motionData ##
    /// 
    public void Save(string name)
    {
        Gesture g = new Gesture();
        g.name = name;
        List<Vector3> fingerData = new List<Vector3>();
        
        // 2 WAYS FOR MOTION CAPTURE: Record for x amount of frames/seconds, or get user input to stop recording?
        List<Vector3> motionData = new List<Vector3>();

        // Record for x amount of time/frames? or until user input to stop recording?
        float recordingTime = 2f; // Set recording time to 2 seconds, finetune later 
        float startTime = Time.time;

        while (Time.time - startTime < recordingTime)
        {
            //Save each individual finger bone in fingerData, save whole hand position in motionData
            foreach (OVRBone bone in fingerBones)
            {
                fingerData.Add(handToRecord.transform.InverseTransformPoint(bone.Transform.position));
            }
            motionData.Add(handToRecord.transform.InverseTransformPoint(handToRecord.transform.position));
        }

        g.fingerData = fingerData;
        g.motionData = motionData;
        g.onRecognized = new UnityEvent();

        g.onRecognized.AddListener(gestureNames[g.name]);

        //Add gesture to Gesture List
        gestures[name] = g;
        print("Saved Gesture " + name);
    }

    //Save gestures in Gesture List as JSON data
    public void GesturesToJSON()
    {
        string json = JsonConvert.SerializeObject(gestures, Formatting.Indented, new JsonSerializerSettings() 
        { 
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore 
        });
        string directory = Application.persistentDataPath + "/GestureRecognitionVR/";
        string saveFile = directory + "savedGestures.json";

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

    //Read json data from existing json files, save in list 
    public void readGesturesFromJSON()
    {
        string directory = Application.persistentDataPath + "/GestureRecognitionVR/";
        string saveFile = directory + "savedGestures.json";

        
        
        if (File.Exists(saveFile))
        {
            string Contents = File.ReadAllText(saveFile);
            gestures = JsonConvert.DeserializeObject<Dictionary<string, Gesture>>(Contents);
        }
        else
        {
            gestures = new Dictionary<string, Gesture>();
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

            // Create Lists to store Velocity and Direction of the motionData (as Vector3s)
            List<Vector3> velocities = new List<Vector3>();
            List<Vector3> directions = new List<Vector3>();

            // Calculate velocity and direction of movement for each item in motionData list
            for (int i = 0; i < kvp.Value.motionData.Count - 1; i++)
            {
                // velocity = displacement / time
                Vector3 displacement = kvp.Value.motionData[i + 1] - kvp.Value.motionData[i];
                Vector3 velocity = displacement / Time.deltaTime;
                velocities.Add(velocity.normalized);
                // Normalize displacement to store vector representing the direction the hand is moving
                directions.Add(displacement.normalized);
            }

            // Comparing finger positions
            for (int i = 0; i < fingerBones.Count; i++)
            {
                Vector3 currentData = handToRecord.transform.InverseTransformPoint(fingerBones[i].Transform.position);
                float fingerDistance = Vector3.Distance(currentData, kvp.Value.fingerData[i]);
                if (fingerDistance > detectionThreshold)
                {
                    discard = true;
                    break;
                }

                sumDistance += fingerDistance;
            }

            //If fingerData is not correct, skip checking motionData
            if (discard)
            {
                continue;
            }

            // Compare velocity and direction vectors for motionData
            for (int i = 0; i < directions.Count; i++)
            {
                //## NOTE: look into dot products and better ways of comparing velocity. Also Vector3.Angle for better comparison of direction vectors.
                // use handToRecord.transform.forward (a vector pointing in direction hand is currently facing) as a reference direction
                // compare the distance of velocity and direction with current hand position
                float directionDistance = Vector3.Distance(directions[i], handToRecord.transform.forward);
                float velocityDistance = Vector3.Distance(velocities[i], handToRecord.transform.forward);
                // by using velocityWeight(set to 0.5 by default), we can adjust how important the velocity of the gesture is
                float combinedDistance = directionDistance + (velocityDistance * velocityWeight);

                if (combinedDistance > detectionThreshold)
                {
                    discard = true;
                    break;
                }

                sumDistance += combinedDistance;
            }

            if (!discard && sumDistance < currentMin)
            {
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