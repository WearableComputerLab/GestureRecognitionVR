using System;
using System.Collections;
using System.Collections.Generic;
using Oculus.Platform;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RecordGameGesture : MonoBehaviour
{
   /// <summary>
   /// Text within the button
   /// </summary>
   private TextMeshPro buttonText;
   
   /// <summary>
   /// Gets the textmeshpro within the button
   /// </summary>
   public void Awake()
   {
      buttonText = GetComponentInChildren<TextMeshPro>();
   }

   /// <summary>
   /// Saves the name of the gesture to be recorded based on the button text and sets the current action to Record
   /// </summary>
   public void OnButtonPressed()
   {
      string name = buttonText.text.Replace("Record ", "").ToLower();
      Debug.Log(name);
      GestureDetect.Instance.userInput = name;
      GestureDetect.Instance.currentAction = StateMachine.InputAction.Record;
   }
}
