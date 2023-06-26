using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static GestureDetect;


public class GestureGame : MonoBehaviour
{

    public GestureDetect gestureDetect;
    public GesturePlayback gesturePlayback;


    private void Start()
    {
        // PlayGame is run when PlayGame button is pressed
       //StartCoroutine(PlayGame());
    }

    public IEnumerator PlayGame()
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

        Gesture? playerGesture = null;


        yield return StartCoroutine(RecognizeForDuration(1f, (gesture) =>
        {
            playerGesture = gesture;
        }));

        // Recognize player gesture and get computer gesture
        //Gesture? playerGesture = Recognize();
        Gesture? computerGesture = GetComputerGesture();

        // Display player and computer gestures (if either are null display "none" to prevent null error)
        Debug.Log($"Player: {(playerGesture != null ? playerGesture.Value.name : "None")} - Computer: {(computerGesture != null ? computerGesture.Value.name : "None")}");

        // Determine the winner
        DetermineWinner(playerGesture, computerGesture);

        yield return new WaitForSeconds(2f);

        // Ask if the player wants to play again (not needed here as user can press button)
        // yield return StartCoroutine(PlayAgain());
    }

    // Coroutine to check for users gesture after countdown
    // Checks for 1 second and if a valid gesture is recognized, if so it is returned to playerGesture
    private IEnumerator RecognizeForDuration(float duration, Action<Gesture?> callback)
    {
        float startTime = Time.time;
        float endTime = startTime + duration;

        while (Time.time < endTime)
        {
            // Run Recognize function from GestureDetect
            Gesture? gesture = gestureDetect.Recognize();
            if (gesture != null)
            {
                callback?.Invoke(gesture);
                yield break;
            }
            yield return null;
        }

        Debug.Log("No gesture recognized within the duration.");
        // Perform some action when no gesture is recognized?
        callback?.Invoke(null);
    }

    // Function for selecting random Rock Paper Scissor gesture for computer player and playing it back
    private Gesture? GetComputerGesture()
    {
        // List of Gestures we want to look for 
        string[] validGestureNames = { "Rock", "Paper", "Scissors" };
        List<string> validGestures = new List<string>();

        // Find valid gestures in gesture list from gestureDetect
        foreach (string gestureName in validGestureNames)
        {
            if (gestureDetect.gestures.ContainsKey(gestureName))
            {
                validGestures.Add(gestureName);
            }
        }

        // If valid gestures are found, randomly select one and play it back on the hand model
        if (validGestures.Count > 0)
        {
            // Select a random gesture
            string randomGestureName = validGestures[UnityEngine.Random.Range(0, validGestures.Count)];
            Gesture randomGesture = gestureDetect.gestures[randomGestureName];
            gesturePlayback.PlayGesture(randomGesture.name);

            return randomGesture;
        }
        // Return null if no valid gestures are found
        else
        {
            Debug.LogWarning("No valid gestures found for rock, paper, or scissors.");
            return null;
        }
    }

    // Logic to determine winner of Rock Paper Scissor game
    private void DetermineWinner(Gesture? playerGesture, Gesture? computerGesture)
    {
        if (playerGesture == null)
        {
            Debug.Log("\nInvalid player gesture!");
            return;
        }

        if (computerGesture == null)
        {
            Debug.Log("\nInvalid computer gesture!");
            return;
        }

        if (playerGesture.Value.name == computerGesture.Value.name)
        {
            Debug.Log("\nIt's a tie!");
        }
        else if ((playerGesture.Value.name == "Rock" && computerGesture.Value.name == "Scissors") ||
                 (playerGesture.Value.name == "Paper" && computerGesture.Value.name == "Rock") ||
                 (playerGesture.Value.name == "Scissors" && computerGesture.Value.name == "Paper"))
        {
            Debug.Log("\nPlayer wins!");
        }
        else
        {
            Debug.Log("\nComputer wins!");
        }
    }

    // This is not needed here as the user can press the PlayGame button to play again
    //private IEnumerator PlayAgain()
    //{
    //    Debug.Log("Do you want to play again? (yes/no)");
    //    string input = Console.ReadLine().ToLower();
    //    if (input == "yes")
    //    {
    //        StartCoroutine(PlayGame());
    //    }
    //    else
    //    {
    //        Debug.Log("Exiting game...");
    //    }

    //    yield return null;
    //}

}

