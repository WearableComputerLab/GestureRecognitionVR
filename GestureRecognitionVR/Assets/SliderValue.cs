using System.Collections;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit.UI;
using TMPro;
using UnityEngine;

public class SliderValue : MonoBehaviour
{
    public int currentValue;
    [SerializeField]private TextMeshPro text;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnValueUpdate(SliderEventData value)
    {
        currentValue = (int)(value.NewValue * 10);
        text.text = currentValue.ToString();
    }
}
