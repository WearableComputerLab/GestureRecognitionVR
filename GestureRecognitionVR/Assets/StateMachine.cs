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

public class StateMachine : MonoBehaviour
{
    /// <summary>
    /// The Current State being handled by the State Machine
    /// </summary>
    private State _currentState;

    /// <summary>
    /// Singleton Instance of the State Machine
    /// </summary>
    public static StateMachine Instance;

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
        Debug.Log($"Starting state: {state.GetType()}");
        yield return state.Start();
        yield return state.End();
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
    public enum InputAction {None, Record, PlayGame}
    
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
            //Search for user Hands
            GestureDetect.Instance.hands = GameObject.FindObjectsOfType<OVRSkeleton>();
            GestureDetect.Instance.FindHandToRecord();

            //If the voice experience is not active, activate it.
            if (!GestureDetect.Instance.appVoiceExperience.Active)
            {
                GestureDetect.Instance.appVoiceExperience.Activate();
            }

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
            if (hasRecognized && (!GestureDetect.Instance.previousGesture.HasValue || !GestureDetect.Instance.currentGesture.Value.Equals(GestureDetect.Instance.previousGesture.Value)))
            {
                Debug.Log("Gesture Recognized: " + GestureDetect.Instance.currentGesture.Value.name);
                GestureDetect.Instance.previousGesture = GestureDetect.Instance.currentGesture;
                GestureDetect.Instance.currentGesture.Value.response.StartRoutine();
            }
            
            //if the current action is not None, break out of the loop and repeat until an appropriate input is found
            if (GestureDetect.Instance.currentAction != InputAction.None)
            {
                break;
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
        yield break;
        
    }

    public override IEnumerator End()
    {
        //TODO: Get finger data from SaveGesture
        GestureDetect.Instance.selectedRecordingTime = float.MinValue;
        GestureDetect.Instance.currentAction = Waiting.InputAction.None;
        if (selectedName == "")
        {
            List<Vector3> currentFrame = new List<Vector3>();
            //TODO: Pass finger data to NameGesture(fingerData)
            //If the duration is not static, record for the specified duration
            if (duration > float.MinValue)
            {
                while (Time.time < duration)
                {
                    //Save each individual finger bone in fingerData, save whole hand position in motionData
                    foreach (OVRBone bone in GestureDetect.Instance.fingerBones)
                    {
                        currentFrame.Add(GestureDetect.Instance.handToRecord.transform.InverseTransformPoint(bone.Transform.position));
                    }
                    /*g.motionData.Add(GestureDetect.Instance.handToRecord.transform.InverseTransformPoint(GestureDetect.Instance.handToRecord.transform.position));
                    
                    
                    // Update count down every second (if motion gesture)
                    if (GestureDetect.Instance.selectedRecordingTime > 0.01)
                    {
                        int currentSecond = Mathf.FloorToInt(Time.time);
                        if (currentSecond > GestureDetect.Instance.lastSecondDisplayed)
                        {
                            float remainingTime = duration - (Time.time - GestureDetect.Instance.startTime);
                            Debug.Log("Recording " + name + "... Time remaining: " +
                                      Mathf.FloorToInt(remainingTime).ToString() + " seconds");
                            lastSecondDisplayed = currentSecond;
                        }
                    }
                    
                    // Save Motion Gestures at 20fps to save resources (fine-tune this)
                    yield return new WaitForSeconds(frameTime);*/
                }
            }
            //If the duration is static, record the frame
            else
            {
                foreach (OVRBone bone in GestureDetect.Instance.fingerBones)
                {
                    currentFrame.Add(GestureDetect.Instance.handToRecord.transform.InverseTransformPoint(bone.Transform.position));
                }
                //g.fingerData.Add(currentFrame);
            }

            StateMachine.SetState(new NameGesture());
        }
        else
        {
            //TODO: If name is not "", prompt for whatever the name is, and reset to PlayGame state. Implement once rest of states are implemented
        }
        
        yield break;
    }
}

/// <summary>
/// State for naming recently recorded gesture to be saved within dictionary
/// </summary>
public class NameGesture : State
{
    public override IEnumerator Start()
    {
        throw new System.NotImplementedException();
    }

    public override IEnumerator End()
    {
        throw new System.NotImplementedException();
    }
}

/// <summary>
/// State for assigning a recently saved gesture to a specific response.
/// </summary>
public class SelectResponse : State
{
    public override IEnumerator Start()
    {
        throw new System.NotImplementedException();
    }

    public override IEnumerator End()
    {
        throw new System.NotImplementedException();
    }
}

/// <summary>
/// State for saving the gesture into dictionary
/// </summary>
public class SaveGesture : State
{
    public override IEnumerator Start()
    {
        throw new System.NotImplementedException();
    }

    public override IEnumerator End()
    {
        throw new System.NotImplementedException();
    }
}