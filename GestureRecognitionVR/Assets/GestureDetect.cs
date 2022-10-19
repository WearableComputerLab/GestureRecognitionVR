using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public struct Gesture
{
    public string name;
    public List<Vector3> fingerDatas;
    public UnityEvent onRecognized;
}

[System.Serializable]
public class SerializableList <T>
{
    public List <T> list;
}

public class GestureDetect : MonoBehaviour
{
    // Gesture Recognition
    [SerializeField]
    private float detectionThreshold = 0.5f;

    // Hands to record
    [SerializeField]
    private OVRSkeleton[] hands;

    [SerializeField]
    private SerializableList<Gesture> gestures;

 // Record new gestures
    [Header("Recording")]
    [SerializeField]
    private OVRSkeleton handToRecord;
    private List<OVRBone> fingerBones = new List<OVRBone>();

    [SerializeField]
    private Gesture currentGesture;

    [SerializeField]
    private Gesture previousGesture;

    [SerializeField]
    public GameObject cube;
    public Renderer cubeRenderer;

    // Start is called before the first frame update
    void Start()
    {
       readGesturesFromJSON();
    }

    // Update is called once per frame
    void Update()
    {
        hands = FindObjectsOfType<OVRSkeleton>();
        findHandtoRecord();
        
        for(int i = 0; i < hands.Length; i++)
        {

        }
        
        //Press Space to record a gesture 
        if(Input.GetKeyDown(KeyCode.Space)){
            Save(); 
            //GesturesToJSON();
        }

        //Check for Recognition (returns recognised Gesture)
        currentGesture = Recognize();
        bool hasRecognized = !currentGesture.Equals(new Gesture());
        //Check if gesture is recognisable and new, log recognised gesture
        if (hasRecognized && !currentGesture.Equals(previousGesture))
        {
            Debug.Log("New Gesture Recognized: " + currentGesture.name);
            previousGesture = currentGesture;
            currentGesture.onRecognized.Invoke();

            //If current gesture is thumbs up, change cube color to green, any other gesture change to red
            if (currentGesture.name == "Thumbs Up")
            {
                cubeRenderer.material.SetColor("_color", Color.green);
            } else { 
                cubeRenderer.material.SetColor("_color", Color.red); 
            }
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
            print(fingerBones.Count);
        }
    }

    /// <summary>
    /// Records a gesture when pressing 'Space' on the keyboard
    /// </summary>
    public void Save()
    {
        Gesture g = new Gesture();
        g.name = "New Gesture";
        List<Vector3> data = new List<Vector3>();
        print(fingerBones.Count);
        foreach(OVRBone bone in fingerBones)
        {
            data.Add(handToRecord.transform.InverseTransformPoint(bone.Transform.position));
            print("Test");
        }
        g.fingerDatas = data;
        gestures.list.Add(g);
    }

    public void GesturesToJSON()
    {
        string json = JsonUtility.ToJson(gestures);
        string saveFile = "savedGestures.json";
        if (File.Exists(saveFile))
        {
            File.Delete(saveFile);
            File.WriteAllText(saveFile, json);
        } 
        else
        {
            File.WriteAllText(saveFile, json);
        }
        
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
        string saveFile = "savedGestures.json";
        if (File.Exists(saveFile)){
            string Contents = File.ReadAllText(saveFile);
            gestures = JsonUtility.FromJson<SerializableList<Gesture>>(Contents);
        }
    }

    Gesture Recognize()
    {
        Gesture currentGesture = new Gesture();
        float currentMin = Mathf.Infinity;

        foreach(Gesture gesture in gestures.list)
        {
            float sumDistance = 0;
            bool discard = false;
            for(int i = 0; i < fingerBones.Count; i++)
            {
                Vector3 currentData = handToRecord.transform.InverseTransformPoint(fingerBones[i].Transform.position);
                float distance = Vector3.Distance(currentData, gesture.fingerDatas[i]);
                if(distance > detectionThreshold)
                {
                    discard = true;
                    break;
                }
                sumDistance += distance;
            }
            if(!discard && sumDistance < currentMin)
            {
                currentMin = sumDistance;
                currentGesture = gesture;
            }
        }
        return currentGesture;
    }
}


#if UNITY_EDITOR
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
#endif