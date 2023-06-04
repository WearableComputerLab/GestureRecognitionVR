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

    public ToggleButton activateVoiceButton;

    /// <summary>
    /// Singleton Instance of the State Machine
    /// </summary>
    public static StateMachine Instance;

    public AppVoiceExperience appVoiceExperienceName;

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
        keyboard = TouchScreenKeyboard.Open("", TouchScreenKeyboardType.Default, false, false, false, false,
            "Enter Name");
        //Debug.Log("Opened Keyboard");
    }
}

/// <summary>
/// Abstract State class being used for State Machine
/// </summary>
public abstract class State
{
    /// <summary>
    /// Runs at the beginning of every State start to allow for functionality to be performed
    /// </summary>
    /// <returns>CoRoutine for Waiting</returns>
    public abstract IEnumerator Start();

    /// <summary>
    /// 
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
    public enum InputAction
    {
        None,
        Record,
        PlayGame
    }

    public override IEnumerator Start()
    {
        yield break;
    }

    public override IEnumerator End()
    {
        // TODO: Wait for input;
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

            if (StateMachine.Instance.activateVoiceButton.isToggled)
            {
                //If the voice experience is not active, activate it.
                if (!GestureDetect.Instance.appVoiceExperience.Active)
                {
                    GestureDetect.Instance.appVoiceExperience.Activate();
                }
            }
            GestureDetect.Instance.durationSlider.SetActive(GestureDetect.Instance.durationSlider.activeSelf && !StateMachine.Instance.activateVoiceButton.isToggled);
            GestureDetect.Instance.recordButton.SetActive(!GestureDetect.Instance.durationSlider.activeSelf && !StateMachine.Instance.activateVoiceButton.isToggled);
            

            //Check for Recognition 20 times a second, same as captured data (returns recognised Gesture if hand is in correct position)
            //NOTE: possible for recognise() to miss start of gesture (fine-tune frequency)
            // if (Time.time > lastUpdateTime + UpdateFrequency)
            // {
            //     currentGesture = Recognize();
            //     lastUpdateTime = Time.time;
            // }
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

    //If no name is passed, the gesture will be saved as a new gesture
    public RecordStart(string name = "")
    {
        selectedName = name;
    }

    //on start, set the duration to the selected recording time (default float.MinValue)
    public override IEnumerator Start()
    {
        duration = GestureDetect.Instance.selectedRecordingTime;
        for (int i = 0; i < 3; i++)
        {
            Debug.Log($"Recording starting in {3-i}");
            yield return new WaitForSeconds(1f);
        }
    }

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

    public NameGesture(List<List<Vector3>> fingerData, List<Vector3> motionData)
    {
        this.fingerData = fingerData;
        this.motionData = motionData;
    }

    public override IEnumerator Start()
    {
        //TODO: Implement Keyboard Input for Naming and TTS to move away from Debug.Log
        GestureDetect.Instance.appVoiceExperience.Deactivate();
        StateMachine.Instance.appVoiceExperienceName.Activate();
        Debug.Log("What would you like to name this gesture?");
        StateMachine.Instance.OpenKeyboard();
        yield break;
    }

    public override IEnumerator End()
    {
        while (true)
        {
            if (string.IsNullOrEmpty(GestureDetect.Instance.userInput))
            {
                if (!StateMachine.Instance.appVoiceExperienceName.Active)
                {
                    StateMachine.Instance.appVoiceExperienceName.Activate();
                }

                yield return new WaitForEndOfFrame();
            }
            else
            {
                break;
            }
        }

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

    public SelectResponse(List<List<Vector3>> fingerData, List<Vector3> motionData, string name)
    {
        this.fingerData = fingerData;
        this.motionData = motionData;
        this.name = name;
    }

    public override IEnumerator Start()
    {
        //TODO: Implement Button Selection for each Response and TTS for Debug.Log
        StateMachine.Instance.appVoiceExperienceName.Deactivate();
        StateMachine.Instance.appVoiceExperienceName.Activate();
        Debug.Log($"Please select which response you would like to assign to {name}.");
        Debug.Log(
            $"Possible Responses: {string.Join(", ", GestureDetect.Instance.responses.Select(response => response.Name()))}");

        buttons = new List<GameObject>();
        //Instantiate Buttons for each item in Response responses
        for (int i = 0; i < GestureDetect.Instance.responses.Count; i++)
        {
            GameObject responseButton = GameObject.Instantiate(GestureDetect.Instance.responseButtonPrefab,
                GestureDetect.Instance.responseButtonPosition.transform);
            responseButton.GetComponent<Interactable>().OnClick.AddListener((() =>
            {
                GestureDetect.Instance.userInput = GestureDetect.Instance.responses[i].Name();
            }));
            responseButton.GetComponentInChildren<TextMeshPro>().text = GestureDetect.Instance.responses[i].Name();
            buttons.Add(responseButton);
        }

        GestureDetect.Instance.responseButtonPosition.GetComponent<GridObjectCollection>().UpdateCollection();

        yield break;
    }

    public override IEnumerator End()
    {
        Response r = null;
        while (true)
        {
            if (string.IsNullOrEmpty(GestureDetect.Instance.userInput))
            {
                if (!StateMachine.Instance.appVoiceExperienceName.Active)
                {
                    StateMachine.Instance.appVoiceExperienceName.Activate();
                }

                yield return new WaitForEndOfFrame();
            }
            else
            {
                if (GestureDetect.Instance.responses.Any(response => string.Equals(response.Name(),
                        GestureDetect.Instance.userInput, StringComparison.CurrentCultureIgnoreCase)))
                {
                    r = GestureDetect.Instance.responses.First(response => string.Equals(response.Name(),
                        GestureDetect.Instance.userInput, StringComparison.CurrentCultureIgnoreCase));
                    break;
                }

                Debug.Log($"Response \"{GestureDetect.Instance.userInput}\" not found. Please try again.");
                GestureDetect.Instance.userInput = "";
            }
        }

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

    public SaveGesture(List<List<Vector3>> fingerData, List<Vector3> motionData, string name, Response response)
    {
        this.fingerData = fingerData;
        this.motionData = motionData;
        this.name = name;
        this.response = response;
    }

    public override IEnumerator Start()
    {
        GestureDetect.Instance.SaveGesture(fingerData, motionData, name, response);
        yield break;
    }

    public override IEnumerator End()
    {
        yield return new WaitForSeconds(1f);
        StateMachine.SetState(new Waiting());
    }
}