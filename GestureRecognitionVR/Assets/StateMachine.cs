using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using UnityEngine;
using Oculus.Voice;
using System.Linq;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Utilities;
using TMPro;

public class StateMachine : MonoBehaviour
{
    /// <summary>
    /// The Current State being handled by the State Machine
    /// </summary>
    private State _currentState;

    /// <summary>
    /// Button for activating and deactivating Voice Recognition
    /// </summary>
    public ToggleButton activateVoiceButton;

    /// <summary>
    /// Singleton Instance of the State Machine
    /// </summary>
    public static StateMachine Instance;

    /// <summary>
    /// Separate AppVoiceExperience to handle Naming and Assigning Responses
    /// </summary>
    public AppVoiceExperience appVoiceExperienceName;

    /// <summary>
    /// Keyboard for user to use when naming Keyboard without Voice Recognition
    /// </summary>
    public TouchScreenKeyboard keyboard;

    /// <summary>
    /// Runs before start to set up Singleton.
    /// </summary>
    private void Awake()
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
    /// Starts Program in StartScene State
    /// </summary>
    private void Start()
    {
        SetState(new StartScene());
    }

    /// <summary>
    /// Sets the current state of the instance to a state and starts a coroutine for that state.
    /// </summary>
    /// <param name="state">The state to be set as the current state</param>
    public static void SetState(State state)
    {
        Instance._currentState = state;
        Instance.StartCoroutine(ManageState(Instance._currentState));
    }

    /// <summary>
    /// Starts a state by calling on its Start and End functions
    /// </summary>
    /// <param name="state">The state to be started</param>
    /// <returns>Start and End WaitForEndOfFrame CoRoutine</returns>
    private static IEnumerator ManageState(State state)
    {
        //Debug.Log($"Starting state: {state.GetType()}");
        yield return state.Start();
        yield return state.End();
    }

    public void OpenKeyboard()
    {
        //Debug.Log("About to Open Keyboard");
        keyboard = TouchScreenKeyboard.Open("", TouchScreenKeyboardType.Default);
        //Debug.Log("Opened Keyboard");
    }
}

/// <summary>
/// Abstract State class being used for State Machine
/// </summary>
public abstract class State
{
    /// <summary>
    /// Runs at the beginning of every State start to set up the necessary functions
    /// </summary>
    /// <returns>CoRoutine for Waiting</returns>
    public abstract IEnumerator Start();

    /// <summary>
    /// Runs after Start() has completed to allow for functionality to be performed
    /// </summary>
    /// <returns></returns>
    public abstract IEnumerator End();
}

/// <summary>
/// Runs on start up to set up the application, including reading in gestures from JSON and creating the Next and Previous buttons
/// </summary>
public class StartScene : State
{
    /// <summary>
    /// Starts the application by reading in gestures from JSON and creating the Next and Previous buttons
    /// </summary>
    /// <returns></returns>
    public override IEnumerator Start()
    {
        //Read any previously saved Gestures from existing json data
        GestureDetect.Instance.ReadGesturesFromJSON();

        //Set 3 default responses at startup
        GestureDetect.Instance.responses = new List<Response>()
        {
            new Sphere(),
            new BlueCube(),
            new BlueSphere()
        };

        //Add listeners to the Next and Previous MRTK buttons
        GestureDetect.Instance.nextButton.OnClick.AddListener(GestureDetect.Instance.NextGesture);
        GestureDetect.Instance.prevButton.OnClick.AddListener(GestureDetect.Instance.PrevGesture);
        yield break;
    }

    /// <summary>
    /// Moves State Machine to Waiting State
    /// </summary>
    /// <returns></returns>
    public override IEnumerator End()
    {
        StateMachine.SetState(new Waiting());
        yield break;
    }
}

/// <summary>
/// State that waits for other states to be referenced such as Record, Next Gesture, Previous Gesture and Recognizing Gestures. 
/// </summary>
public class Waiting : State
{
    /// <summary>
    /// Sets up the Input Actions that the system recognizes upon specific actions
    /// </summary>
    public enum InputAction
    {
        None,
        Record,
        PlayGame
    }

    /// <summary>
    /// Nothing sets up in Waiting Start
    /// </summary>
    /// <returns>yield break</returns>
    public override IEnumerator Start()
    {
        yield break;
    }

    /// <summary>
    /// Waiting State that checks for Voice or user input to move to the appropriate state. Whilst waiting for this InputAction, runs the Recognizing Gestures function and checks for the Next and Previous buttons to be pressed or said.
    /// </summary>
    /// <returns></returns>
    public override IEnumerator End()
    {
        // Based on input, move to specified state
        while (true)
        {
            //if the current action is not None, break out of the loop and repeat until an appropriate input is found
            if (GestureDetect.Instance.currentAction != InputAction.None)
            {
                break;
            }

            //Search for user Hands
            GestureDetect.Instance.hands = GameObject.FindObjectsOfType<OVRSkeleton>();
            GestureDetect.Instance.FindHandToRecord();

            //Checks to see if the voice recognition button is toggled, voice recog disabled until true
            if (StateMachine.Instance.activateVoiceButton.isToggled)
            {
                //If the voice experience is not active, activate it.
                if (!GestureDetect.Instance.appVoiceExperience.Active)
                {
                    GestureDetect.Instance.appVoiceExperience.Activate();
                }
            }

            //If the voice experience is disabled, enable the record button and only show duration slider when record button is clicked and voice experience is disabled.
            GestureDetect.Instance.durationSlider.SetActive(GestureDetect.Instance.durationSlider.activeSelf &&
                                                            !StateMachine.Instance.activateVoiceButton.isToggled);
            GestureDetect.Instance.recordButton.SetActive(!GestureDetect.Instance.durationSlider.activeSelf &&
                                                          !StateMachine.Instance.activateVoiceButton.isToggled);


            //Check for Recognition 20 times a second, same as captured data (returns recognised Gesture if hand is in correct position)
            //NOTE: possible for recognise() to miss start of gesture (fine-tune frequency)
            // if (Time.time > lastUpdateTime + UpdateFrequency)
            // {
            //     currentGesture = Recognize();
            //     lastUpdateTime = Time.time;
            // }
            //Gesture Recognition
            GestureDetect.Instance.currentGesture = GestureDetect.Instance.Recognize();

            bool hasRecognized = GestureDetect.Instance.currentGesture.HasValue;
            //Check if gesture is recognisable and new, log recognised gesture
            if (hasRecognized && (!GestureDetect.Instance.previousGesture.HasValue ||
                                  !GestureDetect.Instance.currentGesture.Value.Equals(GestureDetect.Instance
                                      .previousGesture.Value)))
            {
                Debug.Log("Gesture Recognized: " + GestureDetect.Instance.currentGesture.Value.name);
                GestureDetect.Instance.previousGesture = GestureDetect.Instance.currentGesture;
                GestureDetect.Instance.currentGesture.Value.response.StartRoutine();
            }

            yield return new WaitForEndOfFrame();
        }

        //If the current action is Record, move to the Record State
        StateMachine.SetState(new RecordStart());
    }
}

/// <summary>
/// State for the beginning of the recording process. Check the duration and grab gesture finger data
/// </summary>
public class RecordStart : State
{
    private string selectedName;
    private float duration;

    /// <summary>
    /// If no name is passed, the gesture will be saved as a new gesture
    /// </summary>
    /// <param name="name">name of the gesture being recorded</param>
    public RecordStart(string name = "")
    {
        selectedName = name;
    }

    /// <summary>
    /// On Start, set the duration to the selected recording time (default float.MinValue)
    /// </summary>
    /// <returns></returns>
    public override IEnumerator Start()
    {
        //Duration set to the selected recording time specified (Voice or Slider)
        duration = GestureDetect.Instance.selectedRecordingTime;
        //Countdown before recording starts
        for (int i = 0; i < 3; i++)
        {
            Debug.Log($"Recording starting in {3 - i}");
            yield return new WaitForSeconds(1f);
        }
    }

    /// <summary>
    /// Once Start is finished, records the gesture finger data for the specified duration. If no duration is specified or 0 is selected on the slider, the gesture is static.
    /// 
    /// </summary>
    /// <returns>FingerData and Motion Data saved</returns>
    public override IEnumerator End()
    {
        //TODO: Get finger data from SaveGesture
        GestureDetect.Instance.selectedRecordingTime = float.MinValue;
        GestureDetect.Instance.currentAction = Waiting.InputAction.None;
        const float frameTime = 1f / 20f;

        //If no name is passed, the gesture finger data will be saved
        if (selectedName == "")
        {
            List<Vector3> motionData = new List<Vector3>();
            List<List<Vector3>> fingerData = new List<List<Vector3>>();

            //If the duration is not static (motion), record for the specified duration
            if (duration - GestureDetect.staticRecordingTime > 0.005f)
            {
                DateTime start = DateTime.Now;
                double countdown = 0;
                int lastPrint = -1;
                while (countdown < duration)
                {
                    List<Vector3> currentFrame = new List<Vector3>();
                    //Save each individual finger bone in fingerData, save whole hand position in motionData
                    foreach (OVRBone bone in GestureDetect.Instance.fingerBones)
                    {
                        currentFrame.Add(
                            GestureDetect.Instance.handToRecord.transform
                                .InverseTransformPoint(bone.Transform.position));
                    }

                    fingerData.Add(currentFrame);
                    motionData.Add(
                        GestureDetect.Instance.handToRecord.transform.InverseTransformPoint(GestureDetect.Instance
                            .handToRecord.transform.position));

                    // Save Motion Gestures at 20fps to save resources (fine-tune this)
                    yield return new WaitForSeconds(frameTime);

                    //Countdown for length of recording duration specified
                    countdown = (DateTime.Now - start).TotalSeconds;
                    int roundedCount = (int)countdown;
                    if (lastPrint != roundedCount && roundedCount != 0)
                    {
                        Debug.Log($"Time Remaining: {((int)duration) - roundedCount}");
                        lastPrint = roundedCount;
                    }
                }
            }
            //If the duration is static, record the frame
            else
            {
                List<Vector3> currentFrame = new List<Vector3>();
                foreach (OVRBone bone in GestureDetect.Instance.fingerBones)
                {
                    currentFrame.Add(
                        GestureDetect.Instance.handToRecord.transform.InverseTransformPoint(bone.Transform.position));
                    //Debug.Log(currentFrame.Last());
                }

                fingerData.Add(currentFrame);
            }

            //When recording is finished, move to the NameGesture state with the fingerData and motionData
            StateMachine.SetState(new NameGesture(fingerData, motionData));
        }
        else
        {
            //TODO: If name is not "", prompt for whatever the name is, and reset to PlayGame state. !!Implement once rest of states are implemented!!
        }
    }
}

/// <summary>
/// State for naming recently recorded gesture to be saved within dictionary
/// </summary>
public class NameGesture : State
{
    private List<List<Vector3>> fingerData;
    private List<Vector3> motionData;

    /// <summary>
    /// Sets up the fingerData and motionData that has been saved from the RecordStart state
    /// </summary>
    /// <param name="fingerData">Finger Data saved in RecordStart</param>
    /// <param name="motionData">Motion Data saved in RecordStart</param>
    public NameGesture(List<List<Vector3>> fingerData, List<Vector3> motionData)
    {
        this.fingerData = fingerData;
        this.motionData = motionData;
    }

    /// <summary>
    /// Sets up the Keyboard and new AppVoiceExperience that handles user input to name the gesture.
    /// </summary>
    /// <returns></returns>
    public override IEnumerator Start()
    {
        //TODO: Implement Keyboard Input for Naming and TTS to move away from Debug.Log
        GestureDetect.Instance.appVoiceExperience.Deactivate();
        StateMachine.Instance.appVoiceExperienceName.Activate();
        Debug.Log("What would you like to name this gesture?");
        StateMachine.Instance.OpenKeyboard();
        yield break;
    }

    /// <summary>
    /// Waits for an input, if the input is empty, the state will wait until the user inputs a name, via keyboard or voice.
    /// </summary>
    /// <returns>FingerData, MotionData and name</returns>
    public override IEnumerator End()
    {
        //While an input hasn't been received, wait for an input
        while (true)
        {
            //If the user hasn't inputted anything, activate the AppVoiceExperienceName (to make sure it isn't deactivated)
            if (string.IsNullOrEmpty(GestureDetect.Instance.userInput))
            {
                if (StateMachine.Instance.activateVoiceButton.isToggled)
                {
                    if (!StateMachine.Instance.appVoiceExperienceName.Active)
                    {
                        StateMachine.Instance.appVoiceExperienceName.Activate();
                    }
                }
                else
                {
                    //GestureDetect.Instance.userInput = StateMachine.Instance.keyboard.text;
                    GestureDetect.Instance.userInput = "fuck";
                    break;
                }
            }
            else
            {
                break;
            }

            yield return new WaitForEndOfFrame();
        }

        //Set name as the user input, and reset the user input, then move to the SelectResponse state
        string name = GestureDetect.Instance.userInput;
        GestureDetect.Instance.userInput = "";
        StateMachine.SetState(new SelectResponse(fingerData, motionData, name));
    }
}

/// <summary>
/// State for assigning a recently saved gesture to a specific response.
/// </summary>
public class SelectResponse : State
{
    private List<List<Vector3>> fingerData;
    private List<Vector3> motionData;
    private string name;
    private List<GameObject> buttons;

    /// <summary>
    /// Set up the fingerData, motionData and name that has been saved from the NameGesture state
    /// </summary>
    /// <param name="fingerData">FingerData to be saved</param>
    /// <param name="motionData">MotionData to be saved</param>
    /// <param name="name">Name of gesture</param>
    public SelectResponse(List<List<Vector3>> fingerData, List<Vector3> motionData, string name)
    {
        this.fingerData = fingerData;
        this.motionData = motionData;
        this.name = name;
    }

    /// <summary>
    /// Sets up Buttons for non-voice recognition users to interact with to select a response. Prints a list of possible responses to the user to input.
    /// </summary>
    /// <returns>break</returns>
    public override IEnumerator Start()
    {
        //TODO: Implement Button Selection for each Response and TTS for Debug.Log
        GestureDetect.Instance.appVoiceExperience.Deactivate();
        Debug.Log($"Please select which response you would like to assign to {name}.");
        Debug.Log(
            $"Possible Responses: {string.Join(", ", GestureDetect.Instance.responses.Select(response => response.Name()))}");
        if (StateMachine.Instance.activateVoiceButton.isToggled)
        {
            StateMachine.Instance.appVoiceExperienceName.Deactivate();
            StateMachine.Instance.appVoiceExperienceName.Activate();
        }
        else
        {
            buttons = new List<GameObject>();
            //Instantiate Buttons for each item in Response responses
            foreach (Response r in GestureDetect.Instance.responses)
            {
                GameObject responseButton = GameObject.Instantiate(GestureDetect.Instance.responseButtonPrefab,
                    GestureDetect.Instance.responseButtonPosition.transform);
                responseButton.GetComponent<Interactable>().OnClick.AddListener((() =>
                {
                    Debug.Log(r.Name());
                    GestureDetect.Instance.userInput = r.Name();
                }));
                responseButton.GetComponentInChildren<TextMeshPro>().text = r.Name();
                buttons.Add(responseButton);
            }

            GestureDetect.Instance.responseButtonPosition.GetComponent<GridObjectCollection>().UpdateCollection();
        }

        yield break;
    }

    /// <summary>
    /// Waits for user input to assign the gesture to a response. If the user input is empty, the state will wait until the user inputs a response, via button or voice.
    /// </summary>
    /// <returns>FingerData, MotionData, name and response</returns>
    public override IEnumerator End()
    {
        Response r = null;
        while (true)
        {
            if (string.IsNullOrEmpty(GestureDetect.Instance.userInput))
            {
                if (StateMachine.Instance.activateVoiceButton.isToggled)
                {
                    if (!StateMachine.Instance.appVoiceExperienceName.Active)
                    {
                        StateMachine.Instance.appVoiceExperienceName.Activate();
                    }
                }
                else
                {
                    //Check Button press
                }

                yield return new WaitForEndOfFrame();
            }
            else
            {
                //If the response the user input is in the list of responses, set r to that response and break out of the loop
                if (GestureDetect.Instance.responses.Any(response => string.Equals(response.Name(),
                        GestureDetect.Instance.userInput, StringComparison.CurrentCultureIgnoreCase)))
                {
                    r = GestureDetect.Instance.responses.First(response => string.Equals(response.Name(),
                        GestureDetect.Instance.userInput, StringComparison.CurrentCultureIgnoreCase));
                    break;
                }

                //If the response the user input is not in the list of responses, print an error message and reset the user input
                Debug.Log($"Response \"{GestureDetect.Instance.userInput}\" not found. Please try again.");
                GestureDetect.Instance.userInput = "";
            }
        }

        //Reset user input, destroy all buttons and move to the SaveGesture state
        GestureDetect.Instance.userInput = "";
        foreach (GameObject button in buttons)
        {
            GameObject.Destroy(button);
        }

        StateMachine.SetState(new SaveGesture(fingerData, motionData, name, r));
    }
}

/// <summary>
/// State for saving the gesture into dictionary
/// </summary>
public class SaveGesture : State
{
    private List<List<Vector3>> fingerData;
    private List<Vector3> motionData;
    private string name;
    private Response response;

    /// <summary>
    /// Sets up all data from previous states to be saved
    /// </summary>
    /// <param name="fingerData">FingerData to be saved</param>
    /// <param name="motionData">MotionData to be saved</param>
    /// <param name="name">Name of gesture</param>
    /// <param name="response">response assigned to gesture</param>
    public SaveGesture(List<List<Vector3>> fingerData, List<Vector3> motionData, string name, Response response)
    {
        this.fingerData = fingerData;
        this.motionData = motionData;
        this.name = name;
        this.response = response;
    }

    /// <summary>
    /// Saves the gesture into the dictionary with reference to the data and inputs from user
    /// </summary>
    /// <returns>break</returns>
    public override IEnumerator Start()
    {
        GestureDetect.Instance.SaveGesture(fingerData, motionData, name, response);
        yield break;
    }

    /// <summary>
    /// Waits a second and returns to Waiting
    /// </summary>
    /// <returns>Moves back to waiting state</returns>
    public override IEnumerator End()
    {
        yield return new WaitForSeconds(1f);
        StateMachine.SetState(new Waiting());
    }
}