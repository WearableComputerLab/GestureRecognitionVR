using TMPro;
using UnityEngine;

public class ToggleButton : MonoBehaviour
{
    public bool isToggled;
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