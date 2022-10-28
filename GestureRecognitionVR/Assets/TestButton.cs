using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TestButton : MonoBehaviour
{
    public GestureDetect detector;
    public Renderer cubeRenderer;
    public Color pressed;
    public Color idle;

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
    }

    private void OnTriggerEnter(Collider other)
    {
        cubeRenderer.material.color = pressed;
        /*detector.Save();
        detector.GesturesToJSON();*/
    }

    private void OnTriggerExit(Collider other)
    {
        cubeRenderer.material.color = idle;
    }
}