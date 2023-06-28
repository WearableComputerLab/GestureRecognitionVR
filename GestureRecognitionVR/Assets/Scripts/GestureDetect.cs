using System;
using System.Collections.Generic;
using System.IO;
using Meta.WitAi.Json;
using Newtonsoft.Json;
using UnityEngine;
using Oculus.Voice;
using JsonConvert = Newtonsoft.Json.JsonConvert;
using System.Linq;
using System.Xml.Serialization;
using UnityEngine.Serialization;
using Application = UnityEngine.Application;
using TMPro;
using UnityEngine.SceneManagement;

[JsonObject(MemberSerialization.OptIn)]
public struct Gesture
{
    [Newtonsoft.Json.JsonProperty] public string name;
    [Newtonsoft.Json.JsonProperty] public List<Dictionary<string, SerializedBoneData>> fingerData;
    [Newtonsoft.Json.JsonProperty] public string responseName;
    private Response _response;

    public Response response
    {
        get
        {
            if (_response == null)
            {
                string rName = responseName;
                _response = GestureDetect.Instance.responses.FirstOrDefault(r => string.Equals(r.Name(),
                    rName, StringComparison.CurrentCultureIgnoreCase));
            }

            return _response;
        }
        set { _response = value; }
    }

    public Gesture(string name, List<Dictionary<string, SerializedBoneData>> fingerData, Response response)
    {
        this.name = name;
        this.fingerData = fingerData;
        this._response = response;
        this.responseName = response != null ? response.Name() : "";
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

public class SerializedBoneData
{
    public string boneName;
    public Vector3 position;
    public Quaternion rotation;
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
    //public GameObject handModel;
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
    private float detectionThresholdPosition = 1f;

    private float detectionThresholdRotation = 20f;

    /// <summary>
    /// Hands to record
    /// </summary>
    [SerializeField] public OVRSkeleton[] hands;

    /// <summary>
    /// Create List for Gestures
    /// </summary>
    public Dictionary<string, Gesture> gestures => _gestures ??= ReadGesturesFromJSON();

    private Dictionary<string, Gesture> _gestures;

    /// <summary>
    /// Finds hand used to record gestures
    /// </summary>
    [SerializeField] public OVRSkeleton handToRecord;

    /// <summary>
    /// NOTE: fingerBones is currently including all 24 bones in the hand
    /// </summary>
    public List<OVRBone> fingerBones = new List<OVRBone>();

    /// <summary>
    /// lastRecordingTime is used to ensure a gesture isnt recognised as soon as it is recorded.
    /// </summary>
    public float lastRecordTime = 0f;

    /// <summary>
    /// delay of one second after recording before gesture can be recognized
    /// </summary>
    public float delay = 2.0f;

    /// <summary>
    /// isRecording is used to ensure a gesture isnt recognised as soon as it is recorded.
    /// </summary>
    public bool isRecording = false;

    /// <summary>
    /// Text on Table for important messages
    /// </summary>
    public TextMeshProUGUI userMessage;

    /// <summary>
    /// Set recording time default to 0.01 second (user should be able to change this)
    /// </summary>
    public const float staticRecordingTime = 0.01f;

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
    [FormerlySerializedAs("gestureNamerPrefab")]
    public GameObject responseButtonPrefab;

    [FormerlySerializedAs("gestureNamerPosition")]
    public GameObject responseButtonPosition;

    /// <summary>
    /// Voice Recognition Controller
    /// </summary>
    public AppVoiceExperience appVoiceExperience;

    public SliderValue sliderValue;

    public void Awake()
    {
        if (Instance == null)
        {
            DontDestroyOnLoad(gameObject);
            Instance = this;
        }
        else
        {
            Destroy(this);
        }
    }

    public GameObject recordButton;
    public GameObject durationSlider;

    public void OnRecordButtonPressed()
    {
        recordButton.SetActive(false);
        durationSlider.SetActive(true);
    }

    public void OnPlayGameButtonPressed()
    {
        //Debug.Log(SceneManager.GetActiveScene().name);
        currentAction = SceneManager.GetActiveScene().name == "Main"
            ? StateMachine.InputAction.ToGameScene
            : StateMachine.InputAction.ToMainScene;
        //Debug.Log("Current Action: " + currentAction);
    }

    public void OnDurationButtonPressed()
    {
        currentAction = StateMachine.InputAction.Record;
        selectedRecordingTime = sliderValue.currentValue;
        durationSlider.SetActive(false);
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

    public StateMachine.InputAction currentAction = StateMachine.InputAction.None;


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
            // Debug.Log(response["intents"][0]["name"].Value);
            //Switch for Intents
            switch (response["intents"][0]["name"].Value)
            {
                //If "Record" is recognised, call Save function with specified time
                case "record":
                    Debug.Log("Command Recognised: Record");
                    //setting default time to 0.01f as instantiated.
                    float timeNorm = staticRecordingTime;
                    try
                    {
                        //If the time is specified, set the time to the specified time.
                        //Clamps the time to be between 0.01 and 10 seconds to prevent errors/overclocking system.
                        timeNorm = Mathf.Clamp(
                            int.Parse(response["entities"]["wit$duration:duration"][0]["normalized"]["value"]),
                            staticRecordingTime, 10);
                        Debug.Log($"Length Specified: {timeNorm} seconds");
                    }
                    catch
                    {
                        // ignored
                    }

                    currentAction = StateMachine.InputAction.Record;
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
    /// NextGesture:
    /// - Is run when nextButton is pressed
    /// - Checks the gesture list and increments currentGestureIndex by one
    /// - If the end of the gestures list is reached, it cycles back to the beginning
    /// - Sends the gestureName of the gesture at currentGestureIndex to PlayGesture in GesturePlayback
    /// </summary>
    public void NextGesture()
    {
        // If there are no gestures recorded, return or handle the case appropriately
        if (gestures.Count == 0)
        {
            Debug.LogWarning("No gestures recorded.");
            return;
        }

        // currentGestureIndex is used to cycle through recorded gestures
        currentGestureIndex++;
        //If end of gesture list is reached, start from the start
        if (currentGestureIndex >= gestures.Count)
        {
            currentGestureIndex = 0;
        }

        //Get current gesture name, playback the gesture
        Gesture currentGesture = gestures.Values.ElementAt(currentGestureIndex);
        string gestureName = currentGesture.name;
        gesturePlayback.PlayGesture(gestureName);
    }

    /// <summary>
    /// PrevGesture:
    /// - Is run when prevButton is pressed
    /// - Checks the gesture list and decreases currentGestureIndex by one
    /// - If the beginning of the gestures list is reached, it cycles around to the end
    /// - Sends the gestureName of the gesture at currentGestureIndex to PlayGesture in GesturePlayback
    /// </summary>
    public void PrevGesture()
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

    /// <summary>
    /// TODO
    /// </summary>
    public float UpdateFrequency = 0.05f; // 20 times per second (fine-tune along with frameTime in SaveGesture())

    /// <summary>
    /// TODO
    /// </summary>
    public float lastUpdateTime;


    /// <summary>
    /// FindHandToRecord:
    /// - Finds the hand to record and sets finger bones for the hand
    /// </summary>
    public void FindHandToRecord()
    {
        if (hands.Length > 0)
        {
            // Find OVRRightHandPrefab in hands[] array
            handToRecord = hands.FirstOrDefault(hand => hand.transform.name == "OVRRightHandPrefab");
            
            if (handToRecord != null && handToRecord.Bones != null && handToRecord.Bones.Count > 0)
            {
                // Need every bone in hand to determine local position of fingers
                fingerBones = new List<OVRBone>(handToRecord.Bones);
            }
        }
        else
        {
            Debug.Log("no hands");
        }
    }


    /// <summary>
    /// GesturesToJSON:
    /// - Save gestures in Dictionary as JSON data for future use
    /// - Sets the directory and saveFile location/name
    /// - If one doesnt exist it creates it, or overwrites if it exists
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


    /// <summary>
    /// ReadGesturesFromJSON:
    /// - Reads json data from existing json files, saves in gesture dictionary
    /// - Finds json file at correct directory
    /// - Deserializes data and saves those gestures in the gesture dict
    /// </summary>
    public Dictionary<string, Gesture> ReadGesturesFromJSON()
    {
        string directory = Application.persistentDataPath + "/GestureRecognitionVR/";
        string saveFile = directory + "savedGestures.json";


        if (File.Exists(saveFile))
        {
            string contents = File.ReadAllText(saveFile);
            return JsonConvert.DeserializeObject<Dictionary<string, Gesture>>(contents);
        }
        else
        {
            return new Dictionary<string, Gesture>();
        }
    }


    /// <summary>
    /// Recognize:
    /// - Start by iterating over each gesture, both motion and static gestures.
    /// - If static gesture, compare finger positions/rotations of current hand with saved gesture (CompareBoneData method)
    /// - For motion gestures (isMotionGesture), compare the first frame of the recorded motion data with the current frame being played (MatchMotionGestures method).
    /// - If there is a match, increase motionCounter to represent progression through that specific gesture.
    /// - For subsequent frames, compare the next frame of the recorded motion data with the current frame being played.
    /// - If there is a match, increment the counter for the corresponding motion gesture. 
    /// - If the current or next frame doesn't match the recorded motion data, reset the counter or progression value and continue checking for other gestures.
    /// </summary>
    public Gesture? Recognize(Dictionary<string, Gesture> gestures = null)
    {
        //Set gestures dict, set motionCounter to 0
        gestures ??= Instance.gestures;
        Gesture? currentGesture = null;
        float currentMin = Mathf.Infinity;
        int motionCounter = 0;
        

        // Create a dictionary to store finger bones by their bone names (a snapshot of the current position of the user's hand)
        Dictionary<string, OVRBone> fingerBonesDict = new Dictionary<string, OVRBone>();

        // Populate the fingerBonesDict dictionary with all bones in the current hand
        foreach (OVRBone bone in fingerBones)
        {
            string boneName = bone.Transform.name;
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
                            if (!CompareBoneData(gestureBonePosition, gestureBoneRotation, currentBonePosition,
                                    currentBoneRotation))
                            {
                                discard = true;
                                break;
                            }

                            // Calculate the distance between the current frame data and the user's current hand position
                            float positionDistance = Vector3.Distance(currentBonePosition, gestureBonePosition);
                            float rotationAngle = Quaternion.Angle(currentBoneRotation, gestureBoneRotation);

                            // Check if the position or rotation distance exceeds the detection threshold
                            if (positionDistance > detectionThresholdPosition ||
                                rotationAngle > detectionThresholdRotation)
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

                        if (motionCounter >= motionGestureThreshold)
                        {
                            // Motion gesture has been matched completely
                            currentGesture = kvp.Value;
                            return currentGesture;
                        }
                        else if (motionCounter < kvp.Value.fingerData.Count)
                        {
                            Dictionary<string, SerializedBoneData>
                                motionFrameData = kvp.Value.fingerData[motionCounter];
                            if (MatchMotionFrameData(frameData, motionFrameData, handToRecord.transform.position,
                                    handToRecord.transform.rotation))
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

    /// <summary>
    /// MatchMotionFrameData:
    /// - Takes current frameData (current frame) and motionFrameData (frame at current motionCounter), as well as users whole hand position and rotation
    /// - Compares them to check if the frames match (using CompareHandPosition, CompareRotationData & CompareBoneData methods).
    /// </summary>
    /// <param name="frameData"></param>
    /// <param name="motionFrameData"></param>
    /// <param name="currentHandPosition"></param>
    /// <param name="currentHandRotation"></param>
    /// <returns></returns>
    private bool MatchMotionFrameData(Dictionary<string, SerializedBoneData> frameData,
        Dictionary<string, SerializedBoneData> motionFrameData, Vector3 currentHandPosition,
        Quaternion currentHandRotation)
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
        Quaternion motionHandRotation = motionFrameData["HandPosition"].rotation;
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
            if (!CompareBoneData(adjustedBonePosition, frameBoneData.rotation, motionBoneData.position,
                    motionBoneData.rotation))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// CompareBoneData:
    /// - Compares the position and rotation values of two bones against appropriate detectionThreshold
    /// </summary>
    /// <param name="position1"></param>
    /// <param name="rotation1"></param>
    /// <param name="position2"></param>
    /// <param name="rotation2"></param>
    /// <returns></returns>
    private bool CompareBoneData(Vector3 position1, Quaternion rotation1, Vector3 position2, Quaternion rotation2)
    {
        // Compare the bone positions
        if (Vector3.Distance(position1, position2) > detectionThresholdPosition)
        {
            return false;
        }

        // Compare the bone rotations
        if (Quaternion.Angle(rotation1, rotation2) > detectionThresholdRotation)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// CompareHandPosition:
    /// - Method to compare 2 different hand positions using detectionThreshold
    /// </summary>
    /// <param name="handPosition1"></param>
    /// <param name="handPosition2"></param>
    /// <param name="detectionThreshold"></param>
    private bool CompareHandPosition(Vector3 handPosition1, Vector3 handPosition2, float detectionThreshold)
    {
        float positionDistance = Vector3.Distance(handPosition1, handPosition2);

        return positionDistance <= detectionThreshold;
    }

    /// <summary>
    /// CompareRotationData:
    /// - Method to compare 2 different hand rotations
    /// - Change detectionThreshold to 50 degrees for whole hand rotations
    /// </summary>
    /// <param name="rotation1"></param>
    /// <param name="rotation2"></param>
    /// <param name="detectionThreshold"></param>
    private bool CompareRotationData(Quaternion rotation1, Quaternion rotation2, float detectionThreshold)
    {
        detectionThreshold = 50f;

        Quaternion deltaRotation = Quaternion.Inverse(rotation1) * rotation2;
        float rotationAngle = Quaternion.Angle(Quaternion.identity, deltaRotation);

        if (rotationAngle > detectionThreshold)
        {
            //Debug.Log("angle is bigger than threshold");
        }

        return rotationAngle <= detectionThreshold;
    }
}