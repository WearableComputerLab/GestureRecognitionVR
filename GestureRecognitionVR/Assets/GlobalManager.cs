using UnityEngine;

public class GlobalManager : MonoBehaviour
{
    // Singleton instance
    private static GlobalManager instance;

    // Find and assign GestureDetect script 
    public GestureDetect GestureDetect { get; private set; }

    // Accessor for the singleton instance
    public static GlobalManager Instance
    {
        get
        {
            // If the instance is null, try to find an existing GameManager in the scene
            if (instance == null)
            {
                instance = FindObjectOfType<GlobalManager>();

                // If no GameManager is found, create a new one
                if (instance == null)
                {
                    GameObject managerObj = new GameObject("GlobalManager");
                    instance = managerObj.AddComponent<GlobalManager>();
                }
            }

            return instance;
        }
    }

    // Other variables and properties

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

}
