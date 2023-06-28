using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameStateMachine : StateMachine
{
    /// <summary>
    /// Singleton Instance of the State Machine
    /// </summary>
    public static GameStateMachine Instance;

    public static string[] GameGestures = new[] { "rock", "paper", "scissors" };
    
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
    public override IEnumerator Start()
    {
        //GestureDetect.Instance.ReadGesturesFromJSON();
        GestureDetect.Instance.hands = GameObject.FindObjectsOfType<OVRSkeleton>();
        GestureDetect.Instance.FindHandToRecord();
        // TODO: Make these not debug logs but a nice notification or something on the table canvas
        yield break;
    }

    public override IEnumerator End()
    {
        int foundLast = -1;

        GestureDetect.Instance.userInput = "";
        GestureDetect.Instance.currentAction = StateMachine.InputAction.None;

        //While there are not the 3 gestures recorded, wait for user to record gestures
        while (true)
        {
            if (GestureDetect.Instance.currentAction == StateMachine.InputAction.Record)
            {
                break;
            }

            if (GestureDetect.Instance.currentAction == StateMachine.InputAction.ToMainScene)
            {
                break;
            }

            //Creates a list of found names based off of the names array involving rock, paper, scissors and stores when found.
            List<string> found = GameStateMachine.GameGestures
                .Where(name => GestureDetect.Instance.gestures.Any(gesture => gesture.Key.Equals(name))).ToList();

            if (found.Count != foundLast)
                foreach (string name in GameStateMachine.GameGestures)
                    if (!found.Contains(name))
                        Debug.Log($"Could not find {name}, please record gesture.");

            foundLast = found.Count;
            yield return new WaitForEndOfFrame();
            if (found.Count == 3) break;
        }

        if (GestureDetect.Instance.currentAction == StateMachine.InputAction.ToMainScene)
        {
            GameStateMachine.SetState(new ToMainScene());
            GestureDetect.Instance.currentAction = StateMachine.InputAction.None;
        }
        else if (GestureDetect.Instance.userInput != "")
        {
            string userInput = GestureDetect.Instance.userInput;
            GestureDetect.Instance.userInput = "";
            GestureDetect.Instance.currentAction = StateMachine.InputAction.None;
            GameStateMachine.SetState(new RecordStart(userInput));
        }
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
    
    //TODO: Implement State for recording Rock, Paper and Scissors
    public override IEnumerator Start()
    {
        yield break;
    }

    public override IEnumerator End()
    {
        // Display welcome message
        Debug.Log("Welcome to Rock Paper Scissors");
        yield return new WaitForSeconds(1f);

        while (playerScore != 2 || compScore != 2)
        {
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

            // Recognize player gesture and get computer gesture
            DateTime start = DateTime.Now;
            Gesture? playerGesture = null;

            while (true)
            {
                playerGesture = Recognize();
                if (playerGesture != null || (DateTime.Now - start).TotalSeconds > 5)
                    break;
                yield return new WaitForEndOfFrame();
            }

            Gesture computerGesture = GetComputerGesture();

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

        Debug.Log(playerScore == 2 ? "Player Wins!" : "Computer Wins!");
        playerScore = 0;
        compScore = 0;

        GameStateMachine.SetState(new GameWaiting());
    }

    // RUN FOR MULTIPLE FRAMES (COROUTINE?)
    // Use gestureDetect to recognize and return the player's gesture
    private Gesture? Recognize()
    {
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

    // GET THIS BEFORE COUNTDOWN?
    // Select random gesture (from rock paper or scissors) for computer and play it back using gesturePlayback
    private Gesture GetComputerGesture()
    {
        List<string> validGestures = GameStateMachine.GameGestures
            .Where(gestureName => GestureDetect.Instance.gestures.ContainsKey(gestureName)).ToList();

        // Select a random gesture and play it using gesturePlayback
        string randomGestureName = validGestures[UnityEngine.Random.Range(0, validGestures.Count)];
        Gesture randomGesture = GestureDetect.Instance.gestures[randomGestureName];
        //Debug.Log(randomGesture.name);
        GesturePlayback.Instance.PlayGesture(randomGesture.name);

        return randomGesture;
    }

    // Logic to compare player and computer gestures and determine the winner
    private void DetermineWinner(Gesture playerGesture, Gesture computerGesture)
    {
        //If there isn't a player gesture, log error

        //if player and computer gestures are the same, it's a tie
        if (playerGesture.name == computerGesture.name)
        {
            Debug.Log("\nIt's a tie!");
        }
        //If player gesture beats computer gesture, player wins
        else if ((playerGesture.name == "rock" && computerGesture.name == "scissors") ||
                 (playerGesture.name == "paper" && computerGesture.name == "rock") ||
                 (playerGesture.name == "scissors" && computerGesture.name == "paper"))
        {
            Debug.Log("\nPlayer wins round!");
            playerScore++;
            Debug.Log($"Player Score: {playerScore}");
            //PlayerText = $"Player: {playerScore}"
        }
        //Otherwise, computer wins
        else
        {
            Debug.Log("\nComputer wins round!");
            compScore++;
            Debug.Log($"Computer Score: {compScore}");
            //CompText = //Text = $"Comp: {playerScore}"
        }
    }
}

public class GameWaiting : State
{
    public override IEnumerator Start()
    {
        Debug.Log("Would you like to Play Again?");
        yield break;
    }

    public override IEnumerator End()
    {
        GameStateMachine.Instance.playAgainButton.SetActive(true);
        while (true)
        {
            if (GestureDetect.Instance.currentAction != StateMachine.InputAction.None)
            {
                break;
            }

            yield return new WaitForEndOfFrame();
        }
        Debug.Log(GestureDetect.Instance.currentAction);
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

