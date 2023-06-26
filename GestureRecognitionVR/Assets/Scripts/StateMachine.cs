using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Oculus.Voice;
using System.Linq;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Utilities;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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

    public TextMeshProUGUI currentText;


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
        GestureDetect.Instance.userMessage.text = "Welcome";

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
        if (GestureDetect.Instance.nextButton != null)
        {
            GestureDetect.Instance.nextButton.OnClick.AddListener(GestureDetect.Instance.NextGesture);
            GestureDetect.Instance.prevButton.OnClick.AddListener(GestureDetect.Instance.PrevGesture);
        }

        yield break;
    }

    // When PlayGameButton is pressed, change state to GameStart
    public void StartGame()
    {
        StateMachine.SetState(new GameStart());
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
        PreGame,
        Return
    }

    public override IEnumerator Start()
    {
        yield break;
    }

    public override IEnumerator End()
    {
        // Based on input, move to specified state
        while (true)
        {
            //if the current action is not None, break out of the loop and move to the desired state
            if (GestureDetect.Instance.currentAction != InputAction.None)
            {
                break;
            }

            //Search for user Hands
            GestureDetect.Instance.hands = GameObject.FindObjectsOfType<OVRSkeleton>();
            GestureDetect.Instance.FindHandToRecord();

            //If the user has opted for voice recognition:
            if (StateMachine.Instance.activateVoiceButton.isToggled)
            {
                //If the voice experience is not active, activate it.
                if (!GestureDetect.Instance.appVoiceExperience.Active)
                {
                    GestureDetect.Instance.appVoiceExperience.Activate();
                }
            }

            //If user has not opted for voice recognition, set these active as appropriate.
            GestureDetect.Instance.durationSlider.SetActive(GestureDetect.Instance.durationSlider.activeSelf &&
                                                            !StateMachine.Instance.activateVoiceButton.isToggled);
            GestureDetect.Instance.recordButton.SetActive(!GestureDetect.Instance.durationSlider.activeSelf &&
                                                          !StateMachine.Instance.activateVoiceButton.isToggled);

            // Check for Recognition (returns recognized Gesture if hand is in correct position)
            GestureDetect.Instance.currentGesture = GestureDetect.Instance.Recognize();

            bool hasRecognized = GestureDetect.Instance.currentGesture.HasValue;

            // Check if gesture is recognisable and new, log recognized gesture
            if (hasRecognized && (!GestureDetect.Instance.previousGesture.HasValue ||
                                  !GestureDetect.Instance.currentGesture.Value.Equals(GestureDetect.Instance
                                      .previousGesture.Value)))
            {
                //Debug.Log("Gesture Recognized: " + GestureDetect.Instance.currentGesture.Value.name);
                GestureDetect.Instance.userMessage.text =
                    $"Recognized: {GestureDetect.Instance.currentGesture.Value.name}";
                GestureDetect.Instance.previousGesture = GestureDetect.Instance.currentGesture;

                GestureDetect.Instance.currentGesture.Value.response.StartRoutine();
            }

            yield return new WaitForEndOfFrame();
        }

        //Switch case for all possible actions
        switch (GestureDetect.Instance.currentAction)
        {
            case InputAction.None:
                throw new NotImplementedException();
            case InputAction.Record:
                StateMachine.SetState(new RecordStart());
                break;
            case InputAction.PreGame:
                StateMachine.SetState(new PreGame());
                break;
            case InputAction.Return:
                SceneManager.LoadScene("Scenes/Main");
                StateMachine.SetState(new Waiting());
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
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

    //on start, set the duration to the selected recording time (default float.MinValue) and countdown from 3
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
            string boneName = bone.Id.ToString();

            SerializedBoneData boneData = new SerializedBoneData();
            boneData.boneName = bone.Transform.name;
            boneData.position = bone.Transform.localPosition;
            boneData.rotation = bone.Transform.localRotation;

            frameData[boneName] = boneData;
        }

        // Add the hand position data to the frameData dictionary
        Vector3 handPosition = GestureDetect.Instance.hands[2].transform.position;
        Quaternion handRotation = GestureDetect.Instance.hands[2].transform.rotation;
        SerializedBoneData handData = new SerializedBoneData();
        handData.boneName = "hand_R";
        handData.position = handPosition;
        handData.rotation = handRotation;
        frameData["HandPosition"] = handData;

        return frameData;
    }

    public override IEnumerator End()
    {
        GestureDetect.Instance.selectedRecordingTime = float.MinValue;
        GestureDetect.Instance.currentAction = Waiting.InputAction.None;
        const float frameTime = 1f / 20f;

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

            StateMachine.SetState(new NameGesture(fingerData));
        }
        //If name is not empty, save data for specific name
        else
        {
            //TODO: If name is not "", prompt for whatever the name is, and reset to PlayGame state. !!Implement once rest of states are implemented!!
            Dictionary<string, SerializedBoneData> frameData = SaveFrame();
            fingerData.Add(frameData);
            StateMachine.SetState(new SaveGesture(fingerData, selectedName, null));
        }
    }
}

/// <summary>
/// State for naming recently recorded gesture to be saved within dictionary
/// </summary>
public class NameGesture : State
{
    private List<Dictionary<string, SerializedBoneData>> fingerData;

    public NameGesture(List<Dictionary<string, SerializedBoneData>> fingerData)
    {
        this.fingerData = fingerData;
    }

    private TouchScreenKeyboard _keyboard;

    public override IEnumerator Start()
    {
        //TODO: TTS or other to move away from Debug.Log?
        GestureDetect.Instance.appVoiceExperience.Deactivate();
        _keyboard = TouchScreenKeyboard.Open("", TouchScreenKeyboardType.Default,
            false, false, false, false, "Enter Name");
        Debug.Log("What would you like to name this gesture?");
        yield break;
    }

    public override IEnumerator End()
    {
        //Whilst the user input is empty, wait for user input
        while (true)
        {
            //If the user input is empty, wait for user input
            if (string.IsNullOrEmpty(GestureDetect.Instance.userInput))
            {
                //If the voice button is toggled, activate the voice experience
                if (StateMachine.Instance.activateVoiceButton.isToggled)
                {
                    if (!StateMachine.Instance.appVoiceExperienceName.Active)
                    {
                        StateMachine.Instance.appVoiceExperienceName.Activate();
                    }

                    //If the keyboard is active, deactivate it
                    if (_keyboard.active)
                    {
                        _keyboard.active = false;
                    }
                }
                //If the voice button is not toggled, activate the keyboard
                else
                {
                    if (!_keyboard.active) _keyboard.active = true;
                    StateMachine.Instance.appVoiceExperienceName.Deactivate();
                    StateMachine.Instance.currentText.text = _keyboard.text;
                    //If Enter Button is pressed
                    if (_keyboard.status == TouchScreenKeyboard.Status.Done)
                    {
                        GestureDetect.Instance.userInput = _keyboard.text;
                    }
                }

                yield return new WaitForEndOfFrame();
            }
            else
            {
                break;
            }
        }

        //Set name, reset all data and move on to next state
        string name = GestureDetect.Instance.userInput;
        StateMachine.Instance.currentText.text = "";
        GestureDetect.Instance.userInput = "";
        _keyboard = null;
        StateMachine.SetState(new SelectResponse(fingerData, name));
    }
}

/// <summary>
/// State for assigning a recently saved gesture to a specific response.
/// </summary>
public class SelectResponse : State
{
    private List<Dictionary<string, SerializedBoneData>> fingerData;
    private string name;
    private List<GameObject> buttons;

    public SelectResponse(List<Dictionary<string, SerializedBoneData>> fingerData, string name)
    {
        this.fingerData = fingerData;
        this.name = name;
    }

    public override IEnumerator Start()
    {
        //TODO: TTS or similar for Debug.Log
        StateMachine.Instance.appVoiceExperienceName.Deactivate();
        StateMachine.Instance.appVoiceExperienceName.Activate();
        Debug.Log($"Please select which response you would like to assign to {name}.");
        Debug.Log(
            $"Possible Responses: {string.Join(", ", GestureDetect.Instance.responses.Select(response => response.Name()))}");

        buttons = new List<GameObject>();
        //Instantiate Buttons for each item in Response responses and set to user input
        foreach (Response t in GestureDetect.Instance.responses)
        {
            GameObject responseButton = GameObject.Instantiate(GestureDetect.Instance.responseButtonPrefab,
                GestureDetect.Instance.responseButtonPosition.transform);
            responseButton.GetComponent<Interactable>().OnClick.AddListener((() =>
            {
                GestureDetect.Instance.userInput = t.Name();
            }));
            responseButton.GetComponentInChildren<TextMeshPro>().text = t.Name();
            buttons.Add(responseButton);
        }

        //Updates Grid so all 3 buttons are positions and displaying correctly
        GestureDetect.Instance.responseButtonPosition.GetComponent<GridObjectCollection>().UpdateCollection();

        //Hide initially in case of Voice Recognition being enabled.
        foreach (GameObject button in buttons)
        {
            button.SetActive(false);
        }

        yield break;
    }

    public override IEnumerator End()
    {
        Response r = null;
        //Whilst the user input is empty, wait for user input
        while (true)
        {
            //If the user input is empty, wait for user input
            if (string.IsNullOrEmpty(GestureDetect.Instance.userInput))
            {
                //If the voice button is toggled, activate the voice experience
                if (StateMachine.Instance.activateVoiceButton.isToggled)
                {
                    if (!StateMachine.Instance.appVoiceExperienceName.Active)
                    {
                        StateMachine.Instance.appVoiceExperienceName.Activate();
                    }
                }

                //Change button visibility depending on whether voice button is toggled
                buttons.ForEach(button => button.SetActive(!StateMachine.Instance.activateVoiceButton.isToggled));

                yield return new WaitForEndOfFrame();
            }
            else
            {
                //If the user input is a valid response from list of responses, set response to that response
                if (GestureDetect.Instance.responses.Any(response => string.Equals(response.Name(),
                        GestureDetect.Instance.userInput, StringComparison.CurrentCultureIgnoreCase)))
                {
                    r = GestureDetect.Instance.responses.First(response => string.Equals(response.Name(),
                        GestureDetect.Instance.userInput, StringComparison.CurrentCultureIgnoreCase));
                    break;
                }

                //If the user input is not a valid response, reset user input and wait for new input
                Debug.Log($"Response \"{GestureDetect.Instance.userInput}\" not found. Please try again.");
                GestureDetect.Instance.userInput = "";
            }
        }

        //Destroy all buttons, reset user input and move on to next state
        GestureDetect.Instance.userInput = "";
        foreach (GameObject button in buttons)
        {
            GameObject.Destroy(button);
        }

        StateMachine.SetState(new SaveGesture(fingerData, name, r));
    }
}

/// <summary>
/// State for saving the gesture into dictionary
/// </summary>
public class SaveGesture : State
{
    private List<Dictionary<string, SerializedBoneData>> fingerData;
    private string name;
    private Response response;

    public SaveGesture(List<Dictionary<string, SerializedBoneData>> fingerData, string name, Response response)
    {
        this.fingerData = fingerData;
        this.name = name;
        this.response = response;
    }

    public override IEnumerator Start()
    {
        // Add gesture to Gesture List
        GestureDetect.Instance.gestures[name] = new Gesture(name, fingerData, response);
        GestureDetect.Instance.GesturesToJSON();
        yield break;
    }

    public override IEnumerator End()
    {
        //Move back to Waiting
        yield return new WaitForSeconds(1f);
        StateMachine.SetState(new Waiting());
    }
}

/// <summary>
/// State that handles moving the scene from Main to Game when button pressed
/// </summary>
public class ToGameScene : State
{
    public override IEnumerator Start()
    {
        //When appropriate button is pressed, move to Game Scene
        SceneManager.LoadScene("Scenes/Game");
        yield break;
    }

    public override IEnumerator End()
    {
        //Move into PreGame State after 5 seconds to ensure StateMachine has caught up (this number can change)
        yield return new WaitForSeconds(5f);
        StateMachine.SetState(new PreGame());
    }
}

/// <summary>
/// State that will deal with recording gestures for rock paper scissors game
/// </summary>
public class PreGame : State
{
    public override IEnumerator Start()
    {
        // TODO: Make these not debug logs but a nice notification or something on the table canvas
        yield break;
    }


    public override IEnumerator End()
    {
        yield return new WaitForSeconds(2f);
        //While there are not the 3 gestures recorded, wait for user to record gestures
        while (true)
        {
            //Check to see if a gesture is recorded for names "Rock", "Paper" and "Scissors", if not, log error
            foreach (string name in new[] { "rock", "paper", "scissors" })
            {
                bool found = false;
                foreach (KeyValuePair<string, Gesture> gesture in GestureDetect.Instance.gestures)
                {
                    if (gesture.Key.Equals(name))
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    Debug.Log($"Could not find {name}, please record gesture.");
                }
            }

            yield return new WaitForEndOfFrame();
            //StateMachine.SetState(new GameSetup());
        }
        //StateMachine.SetState(new GameSetup());
    }
}


/// <summary>
/// State that sets up the AI for the rock paper scissors game
/// </summary>
public class GameSetup : State
{
    //TODO: Implement State for recording Rock, Paper and Scissors
    public override IEnumerator Start()
    {
        throw new NotImplementedException();
    }

    public override IEnumerator End()
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// State that will control game scores and end game
/// </summary>
public class GameStart : State
{
    //TODO: Implement State for recording Rock, Paper and Scissors
    public override IEnumerator Start()
    {
        Debug.Log("Game time started");
        StateMachine.SetState(new PreGame());
        yield return null;
    }

    public override IEnumerator End()
    {
        // Display welcome message
        Debug.Log("Welcome to Rock Paper Scissors");
        yield return new WaitForSeconds(1f);

        // Display countdown messages
        Debug.Log("Get ready!");
        yield return new WaitForSeconds(2f);

        Debug.Log("Rock...");
        yield return new WaitForSeconds(1f);

        Debug.Log("Paper...");
        yield return new WaitForSeconds(1f);

        Debug.Log("Scissors...");
        yield return new WaitForSeconds(1f);

        Debug.Log("Go!");
        yield return new WaitForSeconds(1f);

        // Recognize player gesture and get computer gesture
        Gesture? playerGesture = Recognize();
        Gesture? computerGesture = GetComputerGesture();

        // Display player and computer gestures
        Debug.Log($"Player: {playerGesture.Value.name} - Computer: {computerGesture.Value.name}");

        // Determine the winner
        DetermineWinner(playerGesture, computerGesture);


        //Debug.Log("GameStart ended");
        yield break;
    }

    // RUN FOR MULTIPLE FRAMES (COROUTINE?)
    // Use gestureDetect to recognize and return the player's gesture
    private Gesture? Recognize()
    {
        if (GestureDetect.Instance != null)
        {
            Gesture? gesture = GestureDetect.Instance.Recognize();
            if (gesture == null)
            {
                Debug.Log("No gesture recognized.");
                // Perform some action when no gesture is recognized (waiting state?)
                StateMachine.SetState(new ExitState());
            }
            else
            {
                return gesture;
            }
        }
        else
        {
            Debug.Log("GestureDetect.Instance is null");
        }

        return null;
    }

    // GET THIS BEFORE COUNTDOWN?
    // Select random gesture (from rock paper or scissors) for computer and play it back using gesturePlayback
    private Gesture? GetComputerGesture()
    {
        string[] validGestureNames = { "Rock", "Paper", "Scissors" };
        List<string> validGestures = new List<string>();

        // Find valid gestures from gestureDetect
        foreach (string gestureName in validGestureNames)
        {
            if (GestureDetect.Instance.gestures.ContainsKey(gestureName))
            {
                validGestures.Add(gestureName);
            }
        }

        if (validGestures.Count > 0)
        {
            // Select a random gesture and play it using gesturePlayback
            string randomGestureName = validGestures[UnityEngine.Random.Range(0, validGestures.Count)];
            Gesture randomGesture = GestureDetect.Instance.gestures[randomGestureName];
            //Debug.Log(randomGesture.name);
            GesturePlayback.Instance.PlayGesture(randomGesture.name);

            return randomGesture;
        }
        else
        {
            Debug.LogWarning("No valid gestures found for rock, paper, or scissors.");
            return null;
        }
    }

    // Logic to compare player and computer gestures and determine the winner
    private void DetermineWinner(Gesture? playerGesture, Gesture? computerGesture)
    {
        //If there isn't a player gesture, log error
        if (playerGesture == null)
        {
            Debug.Log("\nInvalid player gesture!");
            return;
        }

        //if there isn't a computer gesture, log error
        if (computerGesture == null)
        {
            Debug.Log("\nInvalid computer gesture!");
            return;
        }

        //if player and computer gestures are the same, it's a tie
        if (playerGesture.Value.name == computerGesture.Value.name)
        {
            Debug.Log("\nIt's a tie!");
        }
        //If player gesture beats computer gesture, player wins
        else if ((playerGesture.Value.name == "Rock" && computerGesture.Value.name == "Scissors") ||
                 (playerGesture.Value.name == "Paper" && computerGesture.Value.name == "Rock") ||
                 (playerGesture.Value.name == "Scissors" && computerGesture.Value.name == "Paper"))
        {
            Debug.Log("\nPlayer wins!");
        }
        //Otherwise, computer wins
        else
        {
            Debug.Log("\nComputer wins!");
        }
    }

    // Ask user to play again (TODO: change input to voice recog or BUTTONS? instead of Console.Readline (which is used as placeholder))
    private IEnumerator PlayAgain()
    {
        Debug.Log("Do you want to play again? (yes/no)");
        string input = Console.ReadLine().ToLower();
        if (input == "yes")
        {
            StateMachine.SetState(new PreGame());
        }
        else
        {
            StateMachine.SetState(new ExitState());
        }

        yield return null;
    }

    // Exit State when player does not want to play again (back to Waiting state)
    private class ExitState : State
    {
        public override IEnumerator Start()
        {
            Debug.Log("Exiting game...");
            yield return null;
        }

        public override IEnumerator End()
        {
            yield return new WaitForSeconds(1f);
            StateMachine.SetState(new Waiting());
        }
    }
}