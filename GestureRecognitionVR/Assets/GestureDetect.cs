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
    public UnityEvent onRecognized;

    public Gesture(UnityAction func)
    {
        name = "UNNAMED";
        fingerData = null;
        motionData = null;

        onRecognized = new UnityEvent();
        onRecognized.AddListener(func);
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
    // TODO
    public GameObject handModel;
    public Microsoft.MixedReality.Toolkit.UI.Interactable nextButton;
    public Microsoft.MixedReality.Toolkit.UI.Interactable prevButton;
    private int currentGestureIndex = 0;
    public GesturePlayback gesturePlayback;

    /// <summary>
    /// Set detectionThreshold. Smaller threshold = more precise hand detection. Set to 0.5.
    /// </summary>
    [SerializeField] private float detectionThreshold = 0.5f;

    /// <summary>
    /// Hands to record
    /// </summary>
    [SerializeField] private OVRSkeleton[] hands;

    /// <summary>
    /// Create List for Gestures
    /// </summary>
    public Dictionary<string, Gesture> gestures;

    /// <summary>
    /// Finds hand used to record gestures
    /// </summary>
    [Header("Recording")] [SerializeField] private OVRSkeleton handToRecord;

    private List<OVRBone> fingerBones = new List<OVRBone>();

    /// <summary>
    /// lastRecordingTime is used to ensure a gesture isnt recognised as soon as it is recorded.
    /// </summary>
    private float lastRecordTime = 0f;

    /// <summary>
    /// delay of one second after recording before gesture can be recognized
    /// </summary>
    private float delay = 1.0f;

    /// <summary>
    /// isRecording is used to ensure a gesture isnt recognised as soon as it is recorded.
    /// </summary>
    private bool isRecording = false;

    /// <summary>
    /// Set recording time default to 0.01 second (user should be able to change this)
    /// </summary>
    private const float recordingTime = 0.01f;

    /// <summary>
    /// Keep track of which Gesture was most recently recognized
    /// </summary>
    private Gesture? currentGesture;

    private Gesture? previousGesture;

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
    Dictionary<string, UnityAction> gestureNames;

    /// <summary>
    /// TODO
    /// </summary>
    public GameObject gestureNamerPrefab;
    public GameObject gestureNamerPosition;

    /// <summary>
    /// Voice Recognition Controller
    /// </summary>
    public AppVoiceExperience appVoiceExperience;

    /// <summary>
    /// Start is called before the first frame update. Calls readGesturesFromJSON() and sets 3 default gestures.
    /// Creates cube button for each gesture.
    /// </summary>
    void Start()
    {
        //Read any previously saved Gestures from existing json data
        ReadGesturesFromJSON();

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

    /// <summary>
    /// Function to view parsed output of transcript (i.e. time specified)
    /// </summary>
    /// <param name="response">Response being listened to by the Voice Recognition</param>
    public void TranscriptParsed(WitResponseNode response)
    {
        Debug.Log($"Voice Input: {response["text"]}");
        //If the intent is to record and the confidence is high enough, save the gesture
        if (response["intents"][0]["name"].Value == "record" &&
            float.Parse(response["intents"][0]["confidence"]) > 0.95f)
        {
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
                //Debug.Log(timeNorm);
            }
            catch
            {
                // ignored
            }

            Save("Gesture 1", timeNorm);
        }
    }

    /// <summary>
    /// TODO
    /// </summary>
    private void NextGesture()
    {
        // currentGestureIndex is used to cycle through recorded gestures
        currentGestureIndex++;
        //If end of gesture list is reached, start from the start
        if (currentGestureIndex >= gestureNames.Count)
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
    private void PrevGesture()
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
    private float lastUpdateTime;

    /// <summary>
    /// Update is called once per frame. Finds hand to record, checks for recognition, logs recognised gesture.
    /// </summary>
    void Update()
    {
        //Search for user Hands
        hands = FindObjectsOfType<OVRSkeleton>();
        FindHandToRecord();

        //If the voice experience is not active, activate it.
        if (!appVoiceExperience.Active)
        {
            appVoiceExperience.Activate();
        }

        //Check for Recognition 20 times a second, same as captured data (returns recognised Gesture if hand is in correct position)
        //NOTE: possible for recognise() to miss start of gesture (fine-tune frequency)
        // if (Time.time > lastUpdateTime + UpdateFrequency)
        // {
        //     currentGesture = Recognize();
        //     lastUpdateTime = Time.time;
        // }
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


    /// <summary>
    /// Find a hand to record and set finger bones for the hand
    /// </summary>
    private void FindHandToRecord()
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
    public IEnumerator SaveGesture(string name, float recordingTime)
    {
        isRecording = true;
        float startTime = Time.time;
        float frameTime = 1f / 20f; // Capture 20 frames per second (fine-tune along with Update() updateFrequency)

        Gesture g = new Gesture();
        g.name = name;
        g.fingerData = new List<List<Vector3>>();
        g.motionData = new List<Vector3>();
        int lastSecondDisplayed = Mathf.FloorToInt(startTime);

        while (Time.time - startTime < recordingTime)
        {
            List<Vector3> currentFrame = new List<Vector3>();

            //Save each individual finger bone in fingerData, save whole hand position in motionData
            foreach (OVRBone bone in fingerBones)
            {
                currentFrame.Add(handToRecord.transform.InverseTransformPoint(bone.Transform.position));
            }

            // if static, motionData should have length of 1.
            g.fingerData.Add(currentFrame);
            g.motionData.Add(handToRecord.transform.InverseTransformPoint(handToRecord.transform.position));


            // Update count down every second (if motion gesture)
            if (recordingTime > 0.01)
            {
                int currentSecond = Mathf.FloorToInt(Time.time);
                if (currentSecond > lastSecondDisplayed)
                {
                    float remainingTime = recordingTime - (Time.time - startTime);
                    Debug.Log("Recording " + name + "... Time remaining: " +
                              Mathf.FloorToInt(remainingTime).ToString() + " seconds");
                    lastSecondDisplayed = currentSecond;
                }
            }

            // Save Motion Gestures at 20fps to save resources (fine-tune this)
            yield return new WaitForSeconds(frameTime);
        }

        g.onRecognized = new UnityEvent();
        g.onRecognized.AddListener(gestureNames[g.name]);

        // Add gesture to Gesture List
        gestures[name] = g;
        Debug.Log("Saved Gesture " + name);

        // set time when gesture was recorded, set isRecording to false
        lastRecordTime = Time.time;
        isRecording = false;
    }

    /// <summary>
    /// Calls upon SaveGesture coroutine to save a gesture.
    /// </summary>
    /// <param name="name">Name of Gesture</param>
    /// <param name="customTime">Time to recording gesture for, defaults to recordingTime (0.01f)</param>
    public void Save(string name, float customTime = recordingTime)
    {
        StartCoroutine(SaveGesture(name, customTime));
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
    private void OnValidate()
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

    /// <summary>
    /// Starts the G1Routine when "Gesture 1" is recognised
    /// </summary>
    public void G1()
    {
        StartCoroutine(G1Routine());
    }

    /// <summary>
    /// When "Gesture 1" is recognised, change cube color to green for 2 seconds, then back to red.
    /// </summary>
    /// <returns>Returns a WaitForSeconds for 2 seconds</returns>
    public IEnumerator G1Routine()
    {
        //If current gesture has name "Gesture 1", change cube color to green for 2 seconds, then back to red.
        cubeRenderer.material.color = newColour;

        yield return new WaitForSeconds(2);

        cubeRenderer.material.color = oldColour;
    }

    /// <summary>
    /// Starts the G2Routine when "Gesture 2" is recognised
    /// </summary>
    public void G2()
    {
        StartCoroutine(G2Routine());
    }

    /// <summary>
    /// When "Gesture 2" is recognised, change cube to a sphere for 2 seconds, then back to cube.
    /// </summary>
    /// <returns>Returns a WaitForSeconds for 2 seconds</returns>
    public IEnumerator G2Routine()
    {
        //If current gesture has name "Gesture 2", change cube to a sphere. After 2 seconds, it will change back.
        cube2.SetActive(false);
        sphere.SetActive(true);

        yield return new WaitForSeconds(2);

        cube2.SetActive(true);
        sphere.SetActive(false);
    }

    /// <summary>
    /// Starts the G3Routine when "Gesture 3" is recognised
    /// </summary>
    public void G3()
    {
        StartCoroutine(G3Routine());
    }

    /// <summary>
    /// When "Gesture 3" is recognized, change cube color and change cube to sphere for 2 seconds, then back to original color and cube.
    /// </summary>
    /// <returns>Returns a WaitForSeconds for 2 seconds</returns>
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
    //velocityWeight allows us to finetune how sensitive recognition is concerning speed of gesture performance, could use detectionThreshold instead?
    private float velocityWeight = 0.5f;

    /// <summary>
    /// TODO - LEWIS, COMMENT THIS CODE PLEASE
    /// </summary>
    /// <returns></returns>
    Gesture? Recognize()
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