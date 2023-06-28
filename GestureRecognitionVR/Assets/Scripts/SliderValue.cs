using System.Collections;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit.UI;
using TMPro;
using UnityEngine;

/// <summary>
/// Class for dealing with value on the Duration Slider
/// </summary>
public class SliderValue : MonoBehaviour
{
    /// <summary>
    /// Current value of the slider
    /// </summary>
    public int currentValue;
    
    /// <summary>
    /// Text to display the current value of the slider
    /// </summary>
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
