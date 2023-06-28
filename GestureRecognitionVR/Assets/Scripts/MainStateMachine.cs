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

/// <summary>
/// Class that handles the state machine for the application
/// </summary>
public class MainStateMachine : StateMachine
{
    /// <summary>
    /// Voice button toggler
    /// </summary>
    public ToggleButton activateVoiceButton;

    /// <summary>
    /// Singleton Instance of the State Machine
    /// </summary>
    public static MainStateMachine Instance;

    /// <summary>
    /// App Voice Experience for naming and assigning response
    /// </summary>
    public AppVoiceExperience appVoiceExperienceName;

    /// <summary>
    /// Text for Keyboard input
    /// </summary>
    public TextMeshProUGUI currentText;


    /// <summary>
    /// Runs before start to set up Singleton.
    /// </summary>
    private void Awake()
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
        StateMachine.SetState(state, Instance);
    }
}

/// <summary>
/// Runs on start up to set up the application, including reading in gestures from JSON and creating the Next and Previous buttons
/// </summary>
public class StartScene : State
{
    /// <summary>
    /// Starts the application by setting up the responses and creating the Next and Previous buttons
    /// </summary>
    /// <returns></returns>
    public override IEnumerator Start()
    {
        GestureDetect.Instance.userMessage.text = "Welcome";

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

    /// <summary>
    /// Move into Waiting State
    /// </summary>
    /// <returns>New State</returns>
    public override IEnumerator End()
    {
        MainStateMachine.SetState(new Waiting());
        yield break;
    }
}

/// <summary>
/// State that waits for other states to be referenced such as Record, Next Gesture, Previous Gesture and Recognizing Gestures. 
/// </summary>
public class Waiting : State
{
    public override IEnumerator Start()
    {
        yield break;
    }

    /// <summary>
    /// Waiting for other states to be referenced such as Record and Play Game, or for Next and Previous Buttons to be pressed.
    /// </summary>
    /// <returns>State Change</returns>
    /// <exception cref="NotImplementedException">Should never reach</exception>
    /// <exception cref="ArgumentOutOfRangeException">If currentAction is set to something that doesn't exist</exception>
    public override IEnumerator End()
    {
        //Based on input, move to specified state
        while (true)
        {
            //If the current action is not None, break out of the loop and move to the desired state
            if (GestureDetect.Instance.currentAction != StateMachine.InputAction.None)
            {
                break;
            }

            //Search for user Hands
            GestureDetect.Instance.hands = GameObject.FindObjectsOfType<OVRSkeleton>();
            GestureDetect.Instance.FindHandToRecord();

            //If the user has opted for voice recognition:
            if (MainStateMachine.Instance.activateVoiceButton.isToggled)
            {
                //If the voice experience is not active, activate it.
                if (!GestureDetect.Instance.appVoiceExperience.Active)
                {
                    GestureDetect.Instance.appVoiceExperience.Activate();
                }
            }

            //If user has not opted for voice recognition, set these active as appropriate.
            GestureDetect.Instance.durationSlider.SetActive(GestureDetect.Instance.durationSlider.activeSelf &&
                                                            !MainStateMachine.Instance.activateVoiceButton.isToggled);
            GestureDetect.Instance.recordButton.SetActive(!GestureDetect.Instance.durationSlider.activeSelf &&
                                                          !MainStateMachine.Instance.activateVoiceButton.isToggled);

            // Check for Recognition (returns recognized Gesture if hand is in correct position)
            GestureDetect.Instance.currentGesture = GestureDetect.Instance.Recognize();

            bool hasRecognized = GestureDetect.Instance.currentGesture.HasValue;

            // Check if gesture is recognisable and new, log recognized gesture
            if (hasRecognized && (!GestureDetect.Instance.previousGesture.HasValue ||
                                  !GestureDetect.Instance.currentGesture.Value.Equals(GestureDetect.Instance
                                      .previousGesture.Value)))
            {
                GestureDetect.Instance.userMessage.text =
                    $"Recognized: {GestureDetect.Instance.currentGesture.Value.name}";
                GestureDetect.Instance.previousGesture = GestureDetect.Instance.currentGesture;

                if (GestureDetect.Instance.currentGesture.Value.response != null)
                {
                    GestureDetect.Instance.currentGesture.Value.response.StartRoutine();
                }
            }

            yield return new WaitForEndOfFrame();
        }

        //Switch case for all possible actions
        switch (GestureDetect.Instance.currentAction)
        {
            case StateMachine.InputAction.None:
                throw new NotImplementedException();
            case StateMachine.InputAction.Record:
                MainStateMachine.SetState(new RecordStart());
                break;
            case StateMachine.InputAction.ToGameScene:
                MainStateMachine.SetState(new ToGameScene());
                break;
            case StateMachine.InputAction.ToMainScene:
                MainStateMachine.SetState(new ToMainScene());
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}

/// <summary>
/// State for naming recently recorded gesture to be saved within dictionary
/// </summary>
public class NameGesture : State
{
    /// <summary>
    /// List holding finger data for the gesture
    /// </summary>
    private List<Dictionary<string, SerializedBoneData>> fingerData;

    /// <summary>
    /// Constructor for NameGesture
    /// </summary>
    /// <param name="fingerData">Fingerdata saved from previous State</param>
    public NameGesture(List<Dictionary<string, SerializedBoneData>> fingerData)
    {
        this.fingerData = fingerData;
    }

    /// <summary>
    /// Keyboard for user input
    /// </summary>
    private TouchScreenKeyboard _keyboard;

    /// <summary>
    /// Prompt user to name the gesture and open keyboard.
    /// </summary>
    /// <returns></returns>
    public override IEnumerator Start()
    {
        GestureDetect.Instance.appVoiceExperience.Deactivate();
        _keyboard = TouchScreenKeyboard.Open("", TouchScreenKeyboardType.Default,
            false, false, false, false, "Enter Name");
        Debug.Log("What would you like to name this gesture?");
        yield break;
    }

    /// <summary>
    /// Waits for user to input a name for the gesture
    /// </summary>
    /// <returns></returns>
    public override IEnumerator End()
    {
        //Whilst the user input is empty, wait for user input
        while (true)
        {
            //If the user input is empty, wait for user input
            if (string.IsNullOrEmpty(GestureDetect.Instance.userInput))
            {
                //If the voice button is toggled, activate the voice experience
                if (MainStateMachine.Instance.activateVoiceButton.isToggled)
                {
                    if (!MainStateMachine.Instance.appVoiceExperienceName.Active)
                    {
                        MainStateMachine.Instance.appVoiceExperienceName.Activate();
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
                    MainStateMachine.Instance.appVoiceExperienceName.Deactivate();
                    MainStateMachine.Instance.currentText.text = _keyboard.text;

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
        MainStateMachine.Instance.currentText.text = "";
        GestureDetect.Instance.userInput = "";
        _keyboard = null;
        MainStateMachine.SetState(new SelectResponse(fingerData, name));
    }
}

/// <summary>
/// State for assigning a recently saved gesture to a specific response.
/// </summary>
public class SelectResponse : State
{
    /// <summary>
    /// Finger data for the gesture
    /// </summary>
    private List<Dictionary<string, SerializedBoneData>> fingerData;

    /// <summary>
    /// Name of the gesture
    /// </summary>
    private string name;

    /// <summary>
    /// Buttons for each response
    /// </summary>
    private List<GameObject> buttons;

    /// <summary>
    /// Constructor for SelectResponse
    /// </summary>
    /// <param name="fingerData">Fingerdata of gesture</param>
    /// <param name="name">name of gesture</param>
    public SelectResponse(List<Dictionary<string, SerializedBoneData>> fingerData, string name)
    {
        this.fingerData = fingerData;
        this.name = name;
    }

    /// <summary>
    /// Resets Voice Experience, Creates Response Buttons and prompts user to select a response
    /// </summary>
    /// <returns>Response</returns>
    public override IEnumerator Start()
    {
        //TODO: TTS or similar for Debug.Log
        MainStateMachine.Instance.appVoiceExperienceName.Deactivate();
        MainStateMachine.Instance.appVoiceExperienceName.Activate();
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

    /// <summary>
    /// Waits for user to select a response and saves it
    /// </summary>
    /// <returns></returns>
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
                if (MainStateMachine.Instance.activateVoiceButton.isToggled)
                {
                    if (!MainStateMachine.Instance.appVoiceExperienceName.Active)
                    {
                        MainStateMachine.Instance.appVoiceExperienceName.Activate();
                    }
                }

                //Change button visibility depending on whether voice button is toggled
                buttons.ForEach(button => button.SetActive(!MainStateMachine.Instance.activateVoiceButton.isToggled));

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

        MainStateMachine.SetState(new SaveGesture(fingerData, name, r));
    }
}

/// <summary>
/// State for saving the gesture into dictionary
/// </summary>
public class SaveGesture : State
{
    /// <summary>
    /// finger data for the gesture
    /// </summary>
    private List<Dictionary<string, SerializedBoneData>> fingerData;

    /// <summary>
    /// name of the gesture
    /// </summary>
    private string name;

    /// <summary>
    /// response for the gesture
    /// </summary>
    private Response response;

    /// <summary>
    /// Bool to check if this is the main scene
    /// </summary>
    private bool isMain;

    /// <summary>
    /// Constructor for SaveGesture
    /// </summary>
    /// <param name="fingerData">Fingerdata of Gesture</param>
    /// <param name="name">Name of Gesture</param>
    /// <param name="response">Response of Gesture</param>
    /// <param name="isMain">If Main Scene</param>
    public SaveGesture(List<Dictionary<string, SerializedBoneData>> fingerData, string name, Response response,
        bool isMain = true)
    {
        this.fingerData = fingerData;
        this.name = name;
        this.response = response;
        this.isMain = isMain;
    }

    /// <summary>
    /// Saves the gesture into the dictionary
    /// </summary>
    /// <returns></returns>
    public override IEnumerator Start()
    {
        // Add gesture to Gesture List
        GestureDetect.Instance.gestures[name] = new Gesture(name, fingerData, response);
        GestureDetect.Instance.GesturesToJSON();
        yield break;
    }

    /// <summary>
    /// Checks if this is the main scene and moves to the appropriate state
    /// </summary>
    /// <returns></returns>
    public override IEnumerator End()
    {
        //Move back to Waiting
        yield return new WaitForSeconds(1f);
        if (isMain)
        {
            MainStateMachine.SetState(new Waiting());
        }
        else
        {
            GameStateMachine.SetState(new PreGame());
        }
    }
}

/// <summary>
/// State that handles moving the scene from Main to Game when button pressed
/// </summary>
public class ToGameScene : State
{
    /// <summary>
    /// Loads the Game Scene and activates the Game State Machine
    /// </summary>
    /// <returns>Scene</returns>
    public override IEnumerator Start()
    {
        //When appropriate button is pressed, move to Game Scene
        SceneManager.LoadScene("Scenes/Game");

        if (GameStateMachine.Instance != null)
        {
            GameStateMachine.Instance.gameObject.SetActive(true);
        }

        yield break;
    }

    /// <summary>
    /// Moves to the PreGame State and deactivates the Main State Machine
    /// </summary>
    /// <returns>State</returns>
    public override IEnumerator End()
    {
        //Move into PreGame State after 1 second to ensure StateMachine has caught up (this number can change)
        GameStateMachine.SetState(new PreGame());
        MainStateMachine.Instance.gameObject.SetActive(false);
        yield break;
    }
}

/// <summary>
/// State that handles moving the scene from Game to Main when button pressed
/// </summary>
public class ToMainScene : State
{
    /// <summary>
    /// Loads the Main Scene and activates the Main State Machine
    /// </summary>
    /// <returns></returns>
    public override IEnumerator Start()
    {
        //When appropriate button is pressed, move to Main Scene
        SceneManager.LoadScene("Scenes/Main");
        MainStateMachine.Instance.gameObject.SetActive(true);
        yield break;
    }

    /// <summary>
    /// Moves to the Waiting State and deactivates the Game State Machine
    /// </summary>
    /// <returns></returns>
    public override IEnumerator End()
    {
        //Move into Waiting State
        MainStateMachine.SetState(new Waiting());
        GameStateMachine.Instance.gameObject.SetActive(false);
        yield break;
    }
}