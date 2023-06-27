using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameScoring : MonoBehaviour
{
    public static GameScoring Instance;

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
    }

    private void Awake()
    {
        if (Instance != null) Destroy(Instance);
        Instance = this;
    }
}