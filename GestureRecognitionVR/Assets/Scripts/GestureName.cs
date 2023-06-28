using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GestureName : MonoBehaviour
{
    private string _gestName;
    public TextMeshPro tmpName;
    public Renderer cubeRenderer;
    public Color recording;
    public Color idle;

    public string gestName
    {
        get { return _gestName; }
        set
        {
            _gestName = value;
            tmpName.text = _gestName;
        }
    }

    public void OnTriggerEnter(Collider other)
    {
        cubeRenderer.material.color = recording;
    }

    private void OnTriggerExit(Collider other)
    {
        cubeRenderer.material.color = idle;
    }
}