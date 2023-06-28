using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Class dealing with default state machine
/// </summary>
public abstract class StateMachine : MonoBehaviour
{
    /// <summary>
    /// The Current State being handled by the State Machine
    /// </summary>
    protected State CurrentState;

    /// <summary>
    /// Sets the current state of the instance to a state and starts a coroutine for that state.
    /// </summary>
    /// <param name="state">The state to be set as the current state</param>
    /// <param name="stateMachine">The state machine to be used</param>
    public static void SetState(State state, StateMachine stateMachine)
    {
        stateMachine.CurrentState = state;
        stateMachine.StartCoroutine(ManageState(stateMachine.CurrentState));
    }

    /// <summary>
    /// Starts a state by calling on its Start and End functions
    /// </summary>
    /// <param name="state">The state to be started</param>
    /// <returns>Start and End WaitForEndOfFrame CoRoutine</returns>
    protected static IEnumerator ManageState(State state)
    {
        //Debug.Log($"Starting state: {state.GetType()}");
        yield return state.Start();
        yield return state.End();
    }

    /// <summary>
    /// Input Actions that can be performed by the State Machine
    /// </summary>
    public enum InputAction
    {
        None,
        Record,
        ToGameScene,
        ToMainScene,
        PlayAgain
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
    /// Runs at the end of every State start to allow for functionality to be performed
    /// </summary>
    /// <returns></returns>
    public abstract IEnumerator End();
}

/// <summary>
/// State for the beginning of the recording process. Check the duration and grab gesture finger data
/// </summary>
public class RecordStart : State
{
    /// <summary>
    /// Name of the gesture being recorded
    /// </summary>
    private string selectedName;
    
    /// <summary>
    /// Duration of the recording
    /// </summary>
    private float duration;

    /// <summary>
    /// Assigns selectedName to name, defaulted to empty
    /// </summary>
    /// <param name="name">default empty</param>
    public RecordStart(string name = "")
    {
        selectedName = name;
    }

    /// <summary>
    /// On start, set the duration to the selected recording time (default float.MinValue) and countdown from 3
    /// </summary>
    /// <returns>moves to end</returns>
    public override IEnumerator Start()
    {
        duration = GestureDetect.Instance.selectedRecordingTime;
        for (int i = 0; i < 3; i++)
        {
            Debug.Log($"Recording starting in {3 - i}");
            yield return new WaitForSeconds(1f);
        }
    }

    /// <summary>
    /// Function that holds the saving of the gesture data
    /// </summary>
    /// <returns>frama data that is being saved</returns>
    private static Dictionary<string, SerializedBoneData> SaveFrame()
    {
        //New Dictionary to save bone name and data
        Dictionary<string, SerializedBoneData> frameData = new Dictionary<string, SerializedBoneData>();
        // Save each individual finger bone in fingerData
        foreach (OVRBone bone in GestureDetect.Instance.fingerBones)
        {
            string boneName = bone.Transform.name;

            SerializedBoneData boneData = new SerializedBoneData();
            boneData.boneName = bone.Transform.name;
            boneData.position = bone.Transform.localPosition;
            boneData.rotation = bone.Transform.localRotation;

            frameData[boneName] = boneData;
        }

        // Add the hand position data to the frameData dictionary
        Vector3 handPosition = GestureDetect.Instance.handToRecord.transform.position;
        Quaternion handRotation = GestureDetect.Instance.handToRecord.transform.rotation;
        SerializedBoneData handData = new SerializedBoneData();
        handData.boneName = "hand_R";
        handData.position = handPosition;
        handData.rotation = handRotation;
        frameData["HandPosition"] = handData;

        return frameData;
    }

    /// <summary>
    /// Records the gesture data for the specified duration
    /// </summary>
    /// <returns>fingerdata</returns>
    public override IEnumerator End()
    {
        //Set the selected recording time to the default
        GestureDetect.Instance.selectedRecordingTime = float.MinValue;
        //Set the current action to none
        GestureDetect.Instance.currentAction = StateMachine.InputAction.None;
        //Sets frame time to 1/20th of a second (20fps)
        const float frameTime = 1f / 20f;

        //List of dictionaries to save the finger data
        List<Dictionary<string, SerializedBoneData>> fingerData = new List<Dictionary<string, SerializedBoneData>>();

        //If no name is passed, the gesture finger data will be saved
        if (selectedName == "")
        {
            //If the duration is not static (motion), record for the specified duration
            if (duration - GestureDetect.staticRecordingTime > 0.005f)
            {
                DateTime start = DateTime.Now;
                double countdown = 0;
                int lastPrint = -1;
                //Whilst the countdown is less than the duration, save the frame data
                while (countdown < duration)
                {
                    Dictionary<string, SerializedBoneData> frameData = SaveFrame();

                    // Add the frame data to the fingerData list
                    fingerData.Add(frameData);

                    // Save Motion Gestures at 20fps to save resources (fine-tune this)
                    yield return new WaitForSeconds(frameTime);

                    //Countdown timer for remaining time
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
                Dictionary<string, SerializedBoneData> frameData = SaveFrame();
                fingerData.Add(frameData);
            }

            GestureDetect.Instance.appVoiceExperience.Deactivate();
            MainStateMachine.SetState(new NameGesture(fingerData));
        }
        //If name is not empty, save data for specific name
        else
        {
            Dictionary<string, SerializedBoneData> frameData = SaveFrame();
            fingerData.Add(frameData);
            GameStateMachine.SetState(new SaveGesture(fingerData, selectedName, null, false));
        }
    }
}