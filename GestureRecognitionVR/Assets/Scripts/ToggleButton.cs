using TMPro;
using UnityEngine;

/// <summary>
/// Class for handling the voice recognition toggle button
/// </summary>
public class ToggleButton : MonoBehaviour
{
    /// <summary>
    /// Boolean for whether button is toggled or not
    /// </summary>
    public bool isToggled;
    
    /// <summary>
    /// Text within the button
    /// </summary>
    [SerializeField] private TextMeshPro text;

    /// <summary>
    /// Changes text of button depending on whether button is toggled or not
    /// </summary>
    /// <param name="value">button's toggled value</param>
    public void OnToggle(bool value)
    {
        isToggled = value;
        text.text = $"Voice Recognition: {(isToggled ? "ON" : "OFF")}";
    }
}