using System.Collections.Generic;
using UnityEngine;

public class GlobalManager : MonoBehaviour
{
    // Singleton instance
    private static GlobalManager instance;

    // Find and assign GestureDetect script 
    public GestureDetect GestureDetect { get; private set; }

    // Initialise Dictionary to hold gestures
    private Dictionary<string, Gesture> gestures;

    // Accessor for the singleton instance
    public static GlobalManager Instance
    {
        get
        {
            // If the instance is null, try to find an existing GlobalManager in the scene
            if (instance == null)
            {
                instance = FindObjectOfType<GlobalManager>();

                // If no GlobalManager is found, create a new one
                if (instance == null)
                {
                    GameObject managerObj = new GameObject("GlobalManager");
                    instance = managerObj.AddComponent<GlobalManager>();
                }
            }

            return instance;
        }
    }

    // Awake method ensures GlobalManager only has one instance and is persistant 
    private void Awake()
    {
        // Ensure there is only one instance of the GlobalManager
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        // Keep the GlobalManager object persistent across scenes
        DontDestroyOnLoad(gameObject);

        // Perform any initialization tasks
        Initialize();
    }

    private void Initialize()
    {
        // Perform initialization logic 
        GestureDetect = FindObjectOfType<GestureDetect>();
    }

    // Set gesture dictionary for use in GestureGame (saved here in readGesturesFromJSON function in GestureDetect)
    public void SetGestures(Dictionary<string, Gesture> loadedGestures)
    {
        gestures = loadedGestures;
    }

    // Returns dictionary of gestures loaded from JSON
    public Dictionary<string, Gesture> GetGestures()
    {
        return gestures;
    }

}
