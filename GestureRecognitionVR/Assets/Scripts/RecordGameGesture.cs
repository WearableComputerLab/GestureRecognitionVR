using System;
using System.Collections;
using System.Collections.Generic;
using Oculus.Platform;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RecordGameGesture : MonoBehaviour
{
   private TextMeshPro buttonText;
   public void Awake()
   {
      buttonText = GetComponentInChildren<TextMeshPro>();
   }

   public void OnButtonPressed()
   {
      string name = buttonText.text.Replace("Record ", "").ToLower();
      Debug.Log(name);
      GestureDetect.Instance.userInput = name;
      GestureDetect.Instance.currentAction = StateMachine.InputAction.Record;
   }
}
