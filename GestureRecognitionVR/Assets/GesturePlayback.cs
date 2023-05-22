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


    public void PlayGesture(string gestureName)
    {
        UpdateGestures();

        // Check if the gestures dictionary is not null and contains the specified gesture
        if (gestures != null && gestures.ContainsKey(gestureName))
        {
            Gesture currentGesture = gestures[gestureName];
            Debug.Log("Current gesture name: " + currentGesture.name);

            // Check if it's a motion gesture
            if (currentGesture.motionData.Count > 1)
            {
                // It's a motion gesture
                Debug.Log("It's a motion gesture");
                StartCoroutine(PlayGestureCoroutine(currentGesture.fingerData, currentGesture.motionData));
            }
            else
            {
                // It's a static gesture
                Debug.Log("It's a static gesture");

                // Find the hand object in the hand model hierarchy
                Transform handObject = handModel.transform.Find("m_ca01_skeleton/hand_R");

                if (handObject != null)
                {
                    int fingerCount = handObject.childCount;

                    // Check if the gesture has finger data frames
                    if (currentGesture.fingerData.Count > 0)
                    {
                        // Iterate over each finger in the gesture data
                        foreach (KeyValuePair<string, List<Vector3>> kvp in currentGesture.fingerData)
                        {
                            string fingerName = kvp.Key;
                            List<Vector3> fingerPositions = kvp.Value;

                            // Find the finger transform in the hand model hierarchy
                            Transform finger = handObject.Find(fingerName);

                            if (finger != null)
                            {
                                // Check if the number of positions matches the number of finger bones
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
                                    Debug.LogWarning("Incorrect number of finger positions in the current gesture frame for finger '" + fingerName + "'. Expected: " + finger.childCount + ", Actual: " + fingerPositions.Count);
                                }
                            }
                            else
                            {
                                Debug.LogWarning("Finger '" + fingerName + "' not found in the hand model hierarchy.");
                            }
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



    IEnumerator PlayGestureCoroutine(Dictionary<string, List<Vector3>> fingerDataFrames, List<Vector3> handMotionFrames)
    {
        foreach (KeyValuePair<string, List<Vector3>> kvp in fingerDataFrames)
        {
            // Retrieve the finger name directly from the dictionary
            string fingerName = kvp.Key;

            // Find the finger transform in the hand model
            Transform finger = handModel.transform.Find(fingerName); 

            if (finger != null)
            {
                // Retrieve the finger positions from the dictionary
                List<Vector3> fingerPositions = kvp.Value;

                // Check if the number of finger positions matches the number of finger bones
                if (fingerPositions.Count == finger.childCount) 
                {
                    // Update the finger bone positions
                    for (int boneIndex = 0; boneIndex < finger.childCount; boneIndex++)
                    {
                        Transform bone = finger.GetChild(boneIndex);
                        Vector3 bonePosition = fingerPositions[boneIndex];
                        // Set the local position of the finger bone
                        bone.localPosition = bonePosition; 
                    }
                }
                else
                {
                    Debug.LogWarning("Incorrect number of finger positions in the current gesture frame for finger: " + fingerName);
                }
            }
            else
            {
                Debug.LogWarning("Unable to find finger: " + fingerName);
            }

            // Wait for a short duration before proceeding to the next frame
            yield return new WaitForSeconds(0.02f); 
        }
    }


}
