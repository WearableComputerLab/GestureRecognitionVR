using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Meta.WitAi.Json;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Events;
using Oculus.Voice;
using JsonConvert = Newtonsoft.Json.JsonConvert;
using System.Linq;

[System.Serializable]
public struct Gesture
{
    public string name;

    public List<List<Vector3>> fingerData;

    // motionData = List of hand position/rotation over time
    public List<Vector3> motionData;
    public Response response;

    public Gesture(string name)
    {
        this.name = name;
        fingerData = null;
        motionData = null;
        response = null;
    }

    /// <summary>
    /// TODO
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
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
    public static GestureDetect Instance;

    // TODO
    public GameObject handModel;
    public Microsoft.MixedReality.Toolkit.UI.Interactable nextButton;
    public Microsoft.MixedReality.Toolkit.UI.Interactable prevButton;
    public int currentGestureIndex = 0;
    public GesturePlayback gesturePlayback;

    /// <summary>
    /// TODO: Set confidence back to 0.95f
    /// Confidence required for a voice command to be recognised
    /// </summary>
    public float confidence = 0.9f;

    /// <summary>
    /// Set detectionThreshold. Smaller threshold = more precise hand detection. Set to 0.5.
    /// </summary>
    [SerializeField] public float detectionThreshold = 0.5f;

    /// <summary>
    /// Hands to record
    /// </summary>
    [SerializeField] public OVRSkeleton[] hands;

    /// <summary>
    /// Create List for Gestures
    /// </summary>
    public Dictionary<string, Gesture> gestures;

    /// <summary>
    /// Finds hand used to record gestures
    /// </summary>
    [Header("Recording")] [SerializeField] public OVRSkeleton handToRecord;

    public List<OVRBone> fingerBones = new List<OVRBone>();

    /// <summary>
    /// lastRecordingTime is used to ensure a gesture isnt recognised as soon as it is recorded.
    /// </summary>
    public float lastRecordTime = 0f;

    /// <summary>
    /// delay of one second after recording before gesture can be recognized
    /// </summary>
    public float delay = 1.0f;

    /// <summary>
    /// isRecording is used to ensure a gesture isnt recognised as soon as it is recorded.
    /// </summary>
    public bool isRecording = false;

    /// <summary>
    /// Set recording time default to 0.01 second (user should be able to change this)
    /// </summary>
    public const float recordingTime = 0.01f;

    public float selectedRecordingTime = float.MinValue;

    /// <summary>
    /// Keep track of which Gesture was most recently recognized
    /// </summary>
    public Gesture? currentGesture;

    public Gesture? previousGesture;

    /// <summary>
    /// Creates cube object and renderer to change color when G1 is recognised (G1Routine). 
    /// </summary>
    [SerializeField] public GameObject cube;

    public Renderer cubeRenderer;
    public Color newColour;
    public Color oldColour;

    /// <summary>
    /// Create second cube, which will be transformed to sphere when G2 is recognised (G2Routine).
    /// </summary>
    [SerializeField] public GameObject cube2;

    /// <summary>
    /// TODO
    /// </summary>
    public GameObject sphere;

    /// <summary>
    /// Create Dictionary to store Gestures
    /// </summary>
    public List<Response> responses;

    /// <summary>
    /// TODO
    /// </summary>
    public GameObject gestureNamerPrefab;

    public GameObject gestureNamerPosition;

    /// <summary>
    /// Voice Recognition Controller
    /// </summary>
    public AppVoiceExperience appVoiceExperience;

    public void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this);
        }
    }

    /// <summary>
    /// Creates Strings for Voice Recognition Status
    /// </summary>
    public string voiceRecog
    {
        get { return _voiceRecog; }
        set { _voiceRecog = value; }
    }

    /// <summary>
    /// String for Voice Recognition Status
    /// </summary>
    private string _voiceRecog;

    public Waiting.InputAction currentAction = Waiting.InputAction.None;


    /// <summary>
    /// Function to view parsed output of transcript (i.e. time specified)
    /// </summary>
    /// <param name="response">Response being listened to by the Voice Recognition</param>
    public void TranscriptParsed(WitResponseNode response)
    {
        Debug.Log($"Voice Input: {response["text"]}");
        //If the confidence exists and is higher than threshold
        if (float.TryParse(response["intents"][0]["confidence"], out float conf) && conf >= confidence)
        {
            Debug.Log(response["intents"][0]["name"].Value);
            //Switch for Intents
            switch (response["intents"][0]["name"].Value)
            {
                //If "Record" is recognised, call Save function with specified time
                case "record":
                    Debug.Log("Command Recognised: Record");
                    //setting default time to 0.01f as instantiated.
                    float timeNorm = recordingTime;
                    try
                    {
                        //If the time is specified, set the time to the specified time.
                        //Clamps the time to be between 0.01 and 10 seconds to prevent errors/overclocking system.
                        timeNorm = Mathf.Clamp(
                            int.Parse(response["entities"]["wit$duration:duration"][0]["normalized"]["value"]),
                            recordingTime, 10);
                        Debug.Log($"Length Specified: {timeNorm} seconds");
                    }
                    catch
                    {
                        // ignored
                    }

                    currentAction = Waiting.InputAction.Record;
                    selectedRecordingTime = timeNorm;

                    // Save("Gesture 1", timeNorm);

                    break;
                //If "Next" is recognised, call NextGesture function
                case "next":
                    NextGesture();
                    break;

                //If "Previous" is recognised, call PrevGesture function
                case "previous":
                    PrevGesture();
                    break;

                //If no command is recognised, log that the command is not recognised.
                default:
                    Debug.Log("Command not recognised");
                    break;
            }
        }
    }

    public string userInput
    {
        get { return _userInput; }
        set { _userInput = value; }
    }

    private string _userInput;


    /// <summary>
    /// TODO
    /// </summary>
    public void NextGesture()
    {
        // currentGestureIndex is used to cycle through recorded gestures
        currentGestureIndex++;
        //If end of gesture list is reached, start from the start
        if (currentGestureIndex >= responses.Count)
        {
            currentGestureIndex = 0;
        }

        //Get current gesture name, playback the gesture
        Gesture currentGesture = gestures.Values.ElementAt(currentGestureIndex);
        string gestureName = currentGesture.name;
        gesturePlayback.PlayGesture(gestureName);
    }

    /// <summary>
    /// TODO 
    /// </summary>
    public void PrevGesture()
    {
        // currentGestureIndex is used to cycle through recorded gestures
        currentGestureIndex--;
        //If user goes back past first gesture, goto end of gesture list
        if (currentGestureIndex < 0)
        {
            currentGestureIndex = gesturePlayback.gestures.Count - 1;
        }

        //Get current gesture name, playback the gesture
        Gesture currentGesture = gestures.Values.ElementAt(currentGestureIndex);
        string gestureName = currentGesture.name;
        gesturePlayback.PlayGesture(gestureName);
    }

    /// <summary>
    /// TODO
    /// </summary>
    public float UpdateFrequency = 0.05f; // 20 times per second (fine-tune along with frameTime in SaveGesture())

    /// <summary>
    /// TODO
    /// </summary>
    public float lastUpdateTime;


    /// <summary>
    /// Find a hand to record and set finger bones for the hand
    /// </summary>
    public void FindHandToRecord()
    {
        if (hands.Length > 0)
        {
            handToRecord = hands[0];
            fingerBones = new List<OVRBone>(handToRecord.Bones);
        }
    }

    // TODO - LEWIS FILL IN YOUR SUMMARY COMMENT PLEASE
    /// <summary>
    /// Save coroutine for motion gestures
    /// </summary>
    /// <param name="name"></param>
    /// <param name="recordingTime"></param>
    /// <returns></returns>
    public void SaveGesture(List<List<Vector3>> fingerData, List<Vector3> motionData, string name, Response response)
    {
        Gesture g = new Gesture(name)
        {
            fingerData = fingerData,
            motionData = motionData,
            response = response
        };

        // Add gesture to Gesture List
        gestures[name] = g;
        Debug.Log($"Saved Gesture {name}");
    }


    /// <summary>
    /// Save gestures in Dictionary as JSON data for future use
    /// </summary>
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
    public void OnValidate()
    {
        // update JSON if any changes to the gesture list have been made
        if (gestures.list.Count > 0)
        {
            GesturesToJSON();
        }
    }
    */

    /// <summary>
    /// Reads json data from existing json files, saves in gesture dictionary
    /// </summary>
    public void ReadGesturesFromJSON()
    {
        string directory = Application.persistentDataPath + "/GestureRecognitionVR/";
        string saveFile = directory + "savedGestures.json";


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

    //Check if current hand gesture is a recorded gesture. NOTE: NOT TESTED
    //velocityWeight allows us to finetune how sensitive recognition is concerning speed of gesture performance, could use detectionThreshold instead?
    public float velocityWeight = 0.5f;

    /// <summary>
    /// TODO - LEWIS, COMMENT THIS CODE PLEASE
    /// </summary>
    /// <returns></returns>
    public Gesture? Recognize()
    {
        Gesture? currentGesture = null;

        // Check that a gesture is not currently being recorded, and that at least one second (delay) has passed from when last gesture was recorded.
        // if (!isRecording && Time.time > lastRecordTime + delay)
        // {
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

            // Compare finger positions for each frame in fingerData
            for (int i = 0; i < kvp.Value.fingerData.Count; i++)
            {
                for (int j = 0; j < fingerBones.Count; j++)
                {
                    Vector3 currentData =
                        handToRecord.transform.InverseTransformPoint(fingerBones[j].Transform.position);
                    float fingerDistance = Vector3.Distance(currentData, kvp.Value.fingerData[i][j]);
                    if (fingerDistance > detectionThreshold)
                    {
                        discard = true;
                        break;
                    }

                    sumDistance += fingerDistance;
                }

                if (discard)
                {
                    break;
                }
            }

            //If fingerData is not correct, skip checking motionData
            if (discard)
            {
                continue;
            }

            // Compare velocity and direction vectors for motionData
            for (int i = 0; i < directions.Count; i++)
            {
                // Use Dot Product of vectors to compare velocity, and Vector Angles to compare direction
                float dotProduct = Vector3.Dot(velocities[i], handToRecord.transform.forward);
                float angle = Vector3.Angle(velocities[i], handToRecord.transform.forward);
                // Get combined 'distance' between vectors, using velocityWeight to determine importance of velocity in gesture
                float combinedDistance = angle + (dotProduct * velocityWeight);

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
        //}

        return currentGesture;
    }
}