using System;
using System.Collections;
using System.Collections.Generic;
using Oculus.Platform;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RecordGameGesture : MonoBehaviour
{
   private TextMeshProUGUI buttonText;
   public void Awake()
   {
      buttonText = GetComponent<TextMeshProUGUI>();
   }

   public void OnButtonPressed()
   {
      string name = buttonText.text.Replace("record ", "");
   }
}
