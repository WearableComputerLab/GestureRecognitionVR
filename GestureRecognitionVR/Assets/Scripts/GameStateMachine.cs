using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Class that will handle the State Machine for the Rock Paper Scissors Game
/// </summary>
public class GameStateMachine : StateMachine
{
    /// <summary>
    /// Singleton Instance of the State Machine
    /// </summary>
    public static GameStateMachine Instance;

    /// <summary>
    /// List of the names of the gestures that will be used in the game
    /// </summary>
    public static string[] GameGestures = new[] { "rock", "paper", "scissors" };

    /// <summary>
    /// Button that will be used to play the game again
    /// </summary>
    public GameObject playAgainButton;

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
    /// Sets the current state of the instance to a state and starts a coroutine for that state.
    /// </summary>
    /// <param name="state">The state to be set as the current state</param>
    public static void SetState(State state)
    {
        StateMachine.SetState(state, Instance);
    }

    /// <summary>
    /// Button Press that changes the current action to PlayAgain
    /// </summary>
    public void OnButtonPressed()
    {
        GestureDetect.Instance.currentAction = StateMachine.InputAction.PlayAgain;
    }
}

/// <summary>
/// State that will deal with recording gestures for rock paper scissors game
/// </summary>
public class PreGame : State
{
    /// <summary>
    /// Grabs the hands from the scene and finds the hand to record
    /// </summary>
    /// <returns></returns>
    public override IEnumerator Start()
    {
        GestureDetect.Instance.hands = GameObject.FindObjectsOfType<OVRSkeleton>();
        GestureDetect.Instance.FindHandToRecord();
        yield break;
    }

    /// <summary>
    /// Checks if the user has recorded the necessary gestures and if not, prompts user to record them.
    /// </summary>
    /// <returns>User Input/List of Gestures</returns>
    public override IEnumerator End()
    {
        int foundLast = -1;

        //Resets the user input and current action
        GestureDetect.Instance.userInput = "";
        GestureDetect.Instance.currentAction = StateMachine.InputAction.None;

        //While there are not the 3 gestures recorded, wait for user to record gestures
        while (true)
        {
            //If the user has pressed the record button, break out of the loop to record gestures
            if (GestureDetect.Instance.currentAction == StateMachine.InputAction.Record)
            {
                break;
            }

            //If the user has pressed the main scene button, break out of the loop to go to main scene
            if (GestureDetect.Instance.currentAction == StateMachine.InputAction.ToMainScene)
            {
                break;
            }

            //Creates a list of found names based off of the names array involving rock, paper, scissors and stores when found.
            List<string> found = GameStateMachine.GameGestures
                .Where(name => GestureDetect.Instance.gestures.Any(gesture => gesture.Key.Equals(name))).ToList();

            //If the number of found gestures is not the same as the last time, log that the gesture was not found and to record it.
            if (found.Count != foundLast)
                foreach (string name in GameStateMachine.GameGestures)
                    if (!found.Contains(name))
                        Debug.Log($"Could not find {name}, please record gesture.");

            //Updates foundLast to the current number of found gestures and waits for the next frame.
            foundLast = found.Count;
            yield return new WaitForEndOfFrame();
            //Breaks out of the loop if the user has recorded all 3 gestures
            if (found.Count == 3) break;
        }

        //If the current action is to go to the main scene, go to the main scene and reset the current action
        if (GestureDetect.Instance.currentAction == StateMachine.InputAction.ToMainScene)
        {
            GameStateMachine.SetState(new ToMainScene());
            GestureDetect.Instance.currentAction = StateMachine.InputAction.None;
        }
        //If a gesture needs to be recorded, set the input based on which button has been pressed and go to record state.
        else if (GestureDetect.Instance.userInput != "")
        {
            string userInput = GestureDetect.Instance.userInput;
            GestureDetect.Instance.userInput = "";
            GestureDetect.Instance.currentAction = StateMachine.InputAction.None;
            GameStateMachine.SetState(new RecordStart(userInput));
        }
        //If the user has recorded all 3 gestures, go to the game start state.
        else
        {
            GameStateMachine.SetState(new GameStart());
        }
    }
}

/// <summary>
/// State that will control game scores and end game
/// </summary>
public class GameStart : State
{
    public int playerScore;
    public int compScore;


    public override IEnumerator Start()
    {
        yield break;
    }

    /// <summary>
    /// Game loop that will run until either the player or computer has 2 points
    /// </summary>
    /// <returns></returns>
    public override IEnumerator End()
    {
        // Display welcome message
        Debug.Log("Welcome to Rock Paper Scissors");
        yield return new WaitForSeconds(1f);

        //While the player or computer has not reached 2 points, run the game loop
        while (playerScore != 2 || compScore != 2)
        {
            //If either the player or computer has reached 2 points, break out of the loop
            if (playerScore == 2 || compScore == 2)
            {
                break;
            }

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

            //Sets up a timer of 5 seconds to allow user to perform gesture.
            DateTime start = DateTime.Now;
            Gesture? playerGesture = null;

            //Whilst the player has not performed a gesture and the time is less than 5 seconds, wait for the next frame.
            while (true)
            {
                playerGesture = Recognize();
                if (playerGesture != null || (DateTime.Now - start).TotalSeconds > 5)
                    break;
                yield return new WaitForEndOfFrame();
            }

            //Randomize the computer's gesture
            Gesture computerGesture = GetComputerGesture();

            //If there isn't a player gesture, log that the gesture was not recognized and go to the game start state.
            if (playerGesture == null)
            {
                Debug.Log("Gesture not recognized.");
                yield return new WaitForSeconds(1f);
                GameStateMachine.SetState(new GameStart());
                yield break;
            }

            // Display player and computer gestures
            Debug.Log($"Player: {playerGesture.Value.name} - Computer: {computerGesture.name}");

            // Determine the winner
            DetermineWinner(playerGesture.Value, computerGesture);
        }

        //When 2 points have been reached, reset score, display the winner and go to the game waiting state.
        Debug.Log(playerScore == 2 ? "Player Wins!" : "Computer Wins!");
        playerScore = 0;
        compScore = 0;
        GameStateMachine.SetState(new GameWaiting());
    }

    /// <summary>
    /// Recognizes the gesture performed by the player based on rock, paper and scissors. If the gesture is not recognized, return null.
    /// </summary>
    /// <returns>Recognized Gesture</returns>
    private Gesture? Recognize()
    {
        //If the GestureDetect.Instance is not null, recognize the gesture based on the gestures in the GameGestures.
        if (GestureDetect.Instance != null)
        {
            Gesture? gesture = GestureDetect.Instance.Recognize(GestureDetect.Instance.gestures
                .Where(g => GameStateMachine.GameGestures.Contains(g.Key)).ToDictionary(g => g.Key, g => g.Value));

            //Debug.Log(gesture.Value.name);
            return gesture != null && GameStateMachine.GameGestures.Any(x => x == gesture.Value.name) ? gesture : null;
        }

        Debug.Log("GestureDetect.Instance is null");

        return null;
    }

    /// <summary>
    /// Randomizes the computer's gesture based on the gestures in the GameGestures.
    /// </summary>
    /// <returns></returns>
    private Gesture GetComputerGesture()
    {
        // Get a list of valid gestures from GameGestures
        List<string> validGestures = GameStateMachine.GameGestures
            .Where(gestureName => GestureDetect.Instance.gestures.ContainsKey(gestureName)).ToList();

        //Select a random gesture and play it using gesturePlayback
        string randomGestureName = validGestures[UnityEngine.Random.Range(0, validGestures.Count)];
        Gesture randomGesture = GestureDetect.Instance.gestures[randomGestureName];
        GesturePlayback.Instance.PlayGesture(randomGesture.name);

        return randomGesture;
    }

    /// <summary>
    /// Logic to determine the winner of the round based on the player and computer gestures.
    /// </summary>
    /// <param name="playerGesture">Player's gesture</param>
    /// <param name="computerGesture">Computer's random</param>
    private void DetermineWinner(Gesture playerGesture, Gesture computerGesture)
    {
        //if player and computer gestures are the same, it's a tie
        if (playerGesture.name == computerGesture.name)
        {
            Debug.Log("\nIt's a tie!");
            Debug.Log($"Player Score: {playerScore}, Computer Score: {compScore}");
        }
        //If player gesture beats computer gesture, player wins
        else if ((playerGesture.name == "rock" && computerGesture.name == "scissors") ||
                 (playerGesture.name == "paper" && computerGesture.name == "rock") ||
                 (playerGesture.name == "scissors" && computerGesture.name == "paper"))
        {
            Debug.Log("\nPlayer wins round!");
            playerScore++;
            Debug.Log($"Player Score: {playerScore}, Computer Score: {compScore}");
        }
        //Otherwise, computer wins
        else
        {
            Debug.Log("\nComputer wins round!");
            compScore++;
            Debug.Log($"Player Score: {playerScore}, Computer Score: {compScore}");
        }
    }
}

/// <summary>
/// Waiting State to determine if the player would like to play again or go back to the main scene.
/// </summary>
public class GameWaiting : State
{
    /// <summary>
    /// Prompts the player to play again or go back to the main scene.
    /// </summary>
    /// <returns></returns>
    public override IEnumerator Start()
    {
        Debug.Log("Would you like to Play Again or return to the Main Menu?");
        yield break;
    }

    /// <summary>
    /// Activates the play again button and waits for the player to select a button.
    /// </summary>
    /// <returns></returns>
    public override IEnumerator End()
    {
        //Activates the play again button.
        GameStateMachine.Instance.playAgainButton.SetActive(true);
        //Waits for the player to select a button.
        while (true)
        {
            if (GestureDetect.Instance.currentAction != StateMachine.InputAction.None)
            {
                break;
            }

            yield return new WaitForEndOfFrame();
        }

        //Switches to the appropriate state based on the player's selection.
        switch (GestureDetect.Instance.currentAction)
        {
            case StateMachine.InputAction.PlayAgain:
                GameStateMachine.SetState(new PreGame());
                GameStateMachine.Instance.playAgainButton.SetActive(false);
                GestureDetect.Instance.currentAction = StateMachine.InputAction.None;
                yield break;
            case StateMachine.InputAction.ToMainScene:
                GameStateMachine.SetState(new ToMainScene());
                GestureDetect.Instance.currentAction = StateMachine.InputAction.None;
                break;
        }
    }
}