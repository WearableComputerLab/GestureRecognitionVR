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

        // Display player and computer gestures
        Debug.Log($"Player: {(playerGesture != null ? playerGesture.Value.name : "None")} - Computer: {(computerGesture != null ? computerGesture.Value.name : "None")}");


        // Determine the winner
        DetermineWinner(playerGesture, computerGesture);

        yield return new WaitForSeconds(2f);

        // Ask if the player wants to play again
        //yield return StartCoroutine(PlayAgain());
    }

    private IEnumerator RecognizeForDuration(float duration, Action<Gesture?> callback)
    {
        float startTime = Time.time;
        float endTime = startTime + duration;

        while (Time.time < endTime)
        {
            Gesture? gesture = gestureDetect.Recognize();
            if (gesture != null)
            {
                callback?.Invoke(gesture);
                yield break;
            }
            yield return null;
        }

        Debug.Log("No gesture recognized within the duration.");
        // Perform some action when no gesture is recognized
        callback?.Invoke(null);
    }

    //private Gesture? Recognize()
    //{
    //    if (gestureDetect != null)
    //    {
    //        Gesture? gesture = gestureDetect.Recognize();
    //        if (gesture == null)
    //        {
    //            Debug.Log("No gesture recognized.");
    //            // Perform some action when no gesture is recognized
    //        }
    //        else
    //        {
    //            return gesture;
    //        }
    //    }
    //    else
    //    {
    //        Debug.Log("GestureDetect.Instance is null");
    //    }

    //    return null;
    //}

    private Gesture? GetComputerGesture()
    {
        string[] validGestureNames = { "Rock", "Paper", "Scissors" };
        List<string> validGestures = new List<string>();

        // Find valid gestures from gestureDetect
        foreach (string gestureName in validGestureNames)
        {
            if (gestureDetect.gestures.ContainsKey(gestureName))
            {
                validGestures.Add(gestureName);
            }
        }

        if (validGestures.Count > 0)
        {
            // Select a random gesture
            string randomGestureName = validGestures[UnityEngine.Random.Range(0, validGestures.Count)];
            Gesture randomGesture = gestureDetect.gestures[randomGestureName];
            gesturePlayback.PlayGesture(randomGesture.name);

            return randomGesture;
        }
        else
        {
            Debug.LogWarning("No valid gestures found for rock, paper, or scissors.");
            return null;
        }
    }

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

