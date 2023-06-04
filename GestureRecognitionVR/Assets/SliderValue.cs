using System.Collections;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit.UI;
using TMPro;
using UnityEngine;

public class SliderValue : MonoBehaviour
{
    public int currentValue;
    [SerializeField]private TextMeshPro text;
    /// <summary>
    /// Changes value on slider by whole numbers (0 to 10)
    /// </summary>
    /// <param name="value">Value of the slider</param>
    public void OnValueUpdate(SliderEventData value)
    {
        currentValue = (int)(value.NewValue * 10);
        text.text = currentValue.ToString();
    }
}
