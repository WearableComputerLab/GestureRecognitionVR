using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.UI;

[System.Serializable]
public struct Gesture
{
    public string name;
    public List<Vector3> fingerDatas;
    public UnityEvent onRecognized;

    public Gesture(UnityAction func)
    {
        name = "UNNAMED";
        fingerDatas = null;

        onRecognized = new UnityEvent();
        onRecognized.AddListener(func);
    }

    public override bool Equals(object obj)
    {
        Vector3 test1 = Vector3.zero;
        Vector3 test2 = Vector3.zero;

        float diff = (test1 - test2).magnitude;
        //if current fingerdata = saved finger data, return.
        //iterate through fingerdata,

        //grab tip of thumb, find distance vector - vector.magnitude
        return base.Equals(obj);
    }
}

[System.Serializable]
public class SerializableList<T>
{
    public List<T> list;
}

public class GestureDetect : MonoBehaviour
{
    // Gesture Recognition
    [SerializeField] private float detectionThreshold = 0.5f;

    // Hands to record
    [SerializeField] private OVRSkeleton[] hands;

    [SerializeField] private SerializableList<Gesture> gestures;

    // Record new gestures
    [Header("Recording")] [SerializeField] private OVRSkeleton handToRecord;
    private List<OVRBone> fingerBones = new List<OVRBone>();

    private Gesture? currentGesture;
    private Gesture? previousGesture;

    [SerializeField] public GameObject cube;
    public Renderer cubeRenderer;
    public Color newColour;
    public Color oldColour;


    [SerializeField] public GameObject cube2;
    public GameObject sphere;


    Dictionary<string, UnityAction> gestureNames;
    public GameObject gestureNamerPrefab;
    public GameObject gestureNamerPosition;

    // Start is called before the first frame update
    void Start()
    {
        readGesturesFromJSON();
        gestureNames = new Dictionary<string, UnityAction>()
        {
            { "Gesture 1", G1 },
            { "Gesture 2", G2 },
            { "Gesture 3", G3 }
        };

        Vector3 currentPos = gestureNamerPosition.transform.position;

        foreach (KeyValuePair<string, UnityAction> keyValuePair in gestureNames)
        {
            GameObject buttonCube = Instantiate(gestureNamerPrefab);
            GestureName gn = buttonCube.GetComponent<GestureName>();
            gn.gestName = keyValuePair.Key;
            gn.gestureDetection = this;
            buttonCube.transform.position = currentPos;
            
            currentPos.x += 0.2f;
        }
    }

    // Update is called once per frame
    void Update()
    {
        hands = FindObjectsOfType<OVRSkeleton>();
        findHandtoRecord();
        /*for(int i = 0; i < hands.Length; i++)
        {

        }*/

        //Press Space to record a gesture - This was replaced with a Button within the Virtual Space
        /*if(Input.GetKeyDown(KeyCode.Space)){
            Save(); 
            //GesturesToJSON();
        }*/

        //Check for Recognition (returns recognised Gesture)
        currentGesture = Recognize();
        bool hasRecognized = currentGesture.HasValue;
        //Check if gesture is recognisable and new, log recognised gesture
        if (hasRecognized && (!previousGesture.HasValue || !currentGesture.Value.Equals(previousGesture.Value)))
        {
            Debug.Log("Gesture Recognized: " + currentGesture.Value.name);
            previousGesture = currentGesture;
            currentGesture.Value.onRecognized.Invoke();
        }
    }

    /// <summary>
    /// Find a hand to record 
    /// </summary>
    private void findHandtoRecord()
    {
        if (hands.Length > 0)
        {
            handToRecord = hands[0];
            fingerBones = new List<OVRBone>(handToRecord.Bones);
        }
    }

    /// <summary>
    /// Records a gesture when Record Button is pressed within Scene
    /// </summary>
    public void Save(string name)
    {
        Gesture g = new Gesture();
        g.name = name;
        List<Vector3> data = new List<Vector3>();

        foreach (OVRBone bone in fingerBones)
        {
            data.Add(handToRecord.transform.InverseTransformPoint(bone.Transform.position));
        }

        g.fingerDatas = data;
        g.onRecognized = new UnityEvent();

        g.onRecognized.AddListener(gestureNames[g.name]);

        gestures.list.Add(g);
        print("Saved Gesture " + name);
    }

    public void GesturesToJSON()
    {
        string json = JsonUtility.ToJson(gestures, true);
        string directory = Application.persistentDataPath + "/GestureRecognitionVR/";
        string saveFile = directory + "savedGestures.json";

        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        if (File.Exists(saveFile))
        {
            File.Delete(saveFile);
        }

        File.WriteAllText(saveFile, json);
    }

    private void OnValidate()
    {
        // update JSON if any changes have been made
        if (gestures.list.Count > 0)
        {
            GesturesToJSON();
        }
    }

    public void readGesturesFromJSON()
    {
        string directory = Application.persistentDataPath + "/GestureRecognitionVR/";
        string saveFile = directory + "savedGestures.json";

        if (File.Exists(saveFile))
        {
            string Contents = File.ReadAllText(saveFile);
            gestures = JsonUtility.FromJson<SerializableList<Gesture>>(Contents);
        }
    }

    public void G1()
    {
        StartCoroutine(G1Routine());
    }

    public IEnumerator G1Routine()
    {
        //If current gesture has name "Gesture 1", change cube color to green, any other gesture change to red
        cubeRenderer.material.color = newColour;

        yield return new WaitForSeconds(2);

        cubeRenderer.material.color = oldColour;
    }

    public void G2()
    {
        StartCoroutine(G2Routine());
    }

    public IEnumerator G2Routine()
    {
        //If current gesture has name "Gesture 2", change cube to a sphere. After 2 seconds, it will change back.
        cube2.SetActive(false);
        sphere.SetActive(true);

        yield return new WaitForSeconds(2);

        cube2.SetActive(true);
        sphere.SetActive(false);
    }

    public void G3()
    {
        StartCoroutine(G3Routine());
    }

    public IEnumerator G3Routine()
    {
        //if current gesture is "Gesture 3", change cube color, and change cube to sphere 
        cubeRenderer.material.color = newColour;
        cube2.SetActive(false);
        sphere.SetActive(true);

        yield return new WaitForSeconds(2);

        cubeRenderer.material.color = oldColour;
        cube2.SetActive(true);
        sphere.SetActive(false);
    }

    Gesture? Recognize()
    {
        Gesture? currentGesture = null;
        float currentMin = Mathf.Infinity;

        foreach (Gesture gesture in gestures.list)
        {
            float sumDistance = 0;
            bool discard = false;
            for (int i = 0; i < fingerBones.Count; i++)
            {
                Vector3 currentData = handToRecord.transform.InverseTransformPoint(fingerBones[i].Transform.position);
                float distance = Vector3.Distance(currentData, gesture.fingerDatas[i]);
                if (distance > detectionThreshold)
                {
                    discard = true;
                    break;
                }

                sumDistance += distance;
            }

            if (!discard && sumDistance < currentMin)
            {
                currentMin = sumDistance;
                currentGesture = gesture;
            }
        }

        return currentGesture;
    }
}

//Inspector Record Button, no longer used.
/*#if UNITY_EDITOR
[CustomEditor(typeof(GestureDetect))]
public class GestureInspector : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GestureDetect gestureDetect = (GestureDetect)target;

        if (GUILayout.Button("Record a Gesture"))
        {
            gestureDetect.Save();
            gestureDetect.GesturesToJSON();
        }
    }
}
#endif*/