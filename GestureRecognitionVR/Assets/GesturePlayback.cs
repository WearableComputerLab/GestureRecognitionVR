using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GesturePlayback : MonoBehaviour
{
    
    public GameObject handModel;
    public GestureDetect gestureDetect;
    public Dictionary<string, Gesture> gestures;

    public void UpdateGestures()
    {
        gestures = new Dictionary<string, Gesture>(gestureDetect.gestures);
    }

    private void Start()
    {
        UpdateGestures();

        // Ensure that gestureDetect is not null and contains gestures
        if (gestureDetect != null && gestureDetect.gestures != null)
        {
            // Assign gestures from gestureDetect
            // gestures = new Dictionary<string, Gesture>(gestureDetect.gestures);
            Debug.Log("gestures list isnt null");
        }
        else
        {
            // Handle the case where gestureDetect or gestureDetect.gestures is null
            Debug.LogWarning("GestureDetect or GestureDetect.gestures is not properly initialized.");
        }
    }

    public void PlayGesture(string gestureName)
    {
        UpdateGestures();

        // Ensure that gestureDetect is not null and contains gestures
        if (gestureDetect != null && gestureDetect.gestures != null)
        {
            Debug.Log("GestureDetect and gestures list are not null");
        }
        else
        {
            // Handle the case where gestureDetect or gestureDetect.gestures is null
            Debug.LogWarning("GestureDetect or GestureDetect.gestures is not properly initialized.");
            return;
        }

        // Check if gestures dictionary is null
        if (gestures != null && gestures.ContainsKey(gestureName))
        {
            // Get the gesture from the dictionary
            Gesture currentGesture = gestures[gestureName];
            Debug.Log("Current gesture name: " + currentGesture.name);

            // Check if gesture is motion or static
            if (currentGesture.motionData.Count > 1)
            {
                // It's a motion gesture...
                Debug.Log("It's a motion gesture");
                StartCoroutine(PlayGestureCoroutine(currentGesture.fingerData, currentGesture.motionData));
            }
            else
            {
                // It's a static gesture...
                Debug.Log("It's a static gesture");

                // Find the "hand_R" object in the hand model
                Transform handObject = handModel.transform.Find("m_ca01_skeleton/hand_R");

                if (handObject != null)
                {
                    int fingerCount = handObject.childCount;

                    // Check if the gesture has enough finger data frames
                    if (currentGesture.fingerData.Count > 0)
                    {
                        // Check if the number of finger data frames matches the number of fingers
                        if (currentGesture.fingerData.Count == fingerCount)
                        {
                            // Update the finger bone positions in the hand object
                            for (int fingerIndex = 0; fingerIndex < fingerCount; fingerIndex++)
                            {
                                Transform finger = handObject.GetChild(fingerIndex);

                                List<Vector3> fingerPositions = currentGesture.fingerData[fingerIndex];

                                // Check if the finger has the expected number of positions
                                if (fingerPositions.Count == finger.childCount)
                                {
                                    // Update the finger bone positions
                                    for (int boneIndex = 0; boneIndex < finger.childCount; boneIndex++)
                                    {
                                        Transform bone = finger.GetChild(boneIndex);

                                        Vector3 bonePosition = fingerPositions[boneIndex];
                                        bone.position = bonePosition;
                                    }
                                }
                                else
                                {
                                    Debug.LogWarning("Incorrect number of finger positions in the current gesture frame for finger index " + fingerIndex + ". Expected: " + finger.childCount + ", Actual: " + fingerPositions.Count);
                                }
                            }
                        }
                        else
                        {
                            Debug.LogWarning("Incorrect number of finger data frames in the current gesture. Expected: " + fingerCount + ", Actual: " + currentGesture.fingerData.Count);
                        }
                    }
                    else
                    {
                        Debug.LogWarning("No finger data frames available for the current gesture.");
                    }
                }
                else
                {
                    Debug.LogWarning("Unable to find the 'hand_R' object in the hand model hierarchy.");
                }
            }
        }
        else
        {
            Debug.LogWarning("Gesture '" + gestureName + "' is not found.");
        }
    }













    IEnumerator PlayGestureCoroutine(List<List<Vector3>> fingerDataFrames, List<Vector3> handMotionFrames)
    {
        for (int i = 0; i < fingerDataFrames.Count; i++)
        {
            // set hand position
            handModel.transform.position = handMotionFrames[i];

            // set finger positions
            for (int j = 0; j < handModel.transform.childCount; j++)
            {
                Transform finger = handModel.transform.GetChild(j);
                Vector3 fingerPosition = fingerDataFrames[i][j];
                finger.position = new Vector3(fingerPosition.x, finger.position.y, finger.position.z);
            }

            yield return new WaitForSeconds(0.02f);
        }
    }

}
