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
            if (currentGesture.motionData != null && currentGesture.motionData.Count > 1)
            {
                // It's a motion gesture
                Debug.Log("It's a motion gesture");
                StartCoroutine(PlayGestureCoroutine(currentGesture.fingerData, currentGesture.motionData));
            }
            else if (currentGesture.fingerData != null)
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
                            Transform finger = FindFingerTransform(handObject, fingerName);

                            if (finger != null)
                            {
                                // Check if the number of positions matches the number of finger bones
                                if (fingerPositions.Count == GetChildBoneCountRecursive(finger))
                                {
                                    // Update the finger bone positions
                                    UpdateFingerBonePositionsRecursive(finger, fingerPositions);
                                }
                                else
                                {
                                    Debug.LogWarning("Incorrect number of finger positions in the current gesture frame for finger '" + fingerName + "'. Expected: " + GetChildBoneCountRecursive(finger) + ", Actual: " + fingerPositions.Count);
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
            else
            {
                Debug.LogWarning("Gesture '" + gestureName + "' has missing finger data or motion data.");
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


    // Recursively find the finger transform by name in the hand model hierarchy
    Transform FindFingerTransform(Transform parentTransform, string fingerName)
    {
        Transform fingerTransform = parentTransform.Find(fingerName);

        if (fingerTransform != null)
        {
            return fingerTransform;
        }
        else
        {
            // If the finger transform is not found at this level, recursively search the child transforms
            for (int i = 0; i < parentTransform.childCount; i++)
            {
                Transform child = parentTransform.GetChild(i);
                fingerTransform = FindFingerTransform(child, fingerName);

                if (fingerTransform != null)
                {
                    return fingerTransform;
                }
            }

            return null; // Finger transform not found in the hierarchy
        }
    }

    // Recursively count the number of child bones under the finger transform
    int GetChildBoneCountRecursive(Transform fingerTransform)
    {
        int count = 0;

        for (int i = 0; i < fingerTransform.childCount; i++)
        {
            Transform child = fingerTransform.GetChild(i);

            // Check if the child transform represents a finger bone
            if (IsFingerBone(child))
            {
                count++;
            }
            else
            {
                // If the child transform has nested finger bones, recursively count them
                count += GetChildBoneCountRecursive(child);
            }
        }

        return count;
    }

    // Recursively update the positions of finger bones based on the finger positions
    void UpdateFingerBonePositionsRecursive(Transform fingerTransform, List<Vector3> fingerPositions)
    {
        for (int i = 0; i < fingerTransform.childCount; i++)
        {
            Transform child = fingerTransform.GetChild(i);

            // Check if the child transform represents a finger bone
            if (IsFingerBone(child))
            {
                int boneIndex = GetBoneIndex(child, fingerPositions);

                // Check if the bone index is within the range of finger positions
                if (boneIndex >= 0 && boneIndex < fingerPositions.Count)
                {
                    Vector3 bonePosition = fingerPositions[boneIndex];
                    child.position = bonePosition;
                }
                else
                {
                    Debug.LogWarning("Invalid bone index for finger '" + fingerTransform.name + "'. Expected: " + boneIndex + ", Actual: " + fingerPositions.Count);
                }
            }
            else
            {
                // If the child transform has nested finger bones, recursively update their positions
                UpdateFingerBonePositionsRecursive(child, fingerPositions);
            }
        }
    }


    // Check if a transform represents a finger bone
    bool IsFingerBone(Transform transform)
    {
        // Customize this check based on your hand model hierarchy
        // For example, you can check for a specific naming pattern or tag
        return transform.name.EndsWith("_R");
    }

    // Get the bone index based on a finger bone transform
    private int GetBoneIndex(Transform boneTransform, List<Vector3> gestureBonePositions)
    {
        Vector3 bonePosition = boneTransform.position;
        float minDistance = float.MaxValue;
        int boneIndex = -1;

        for (int i = 0; i < gestureBonePositions.Count; i++)
        {
            float distance = Vector3.Distance(bonePosition, gestureBonePositions[i]);
            if (distance < minDistance)
            {
                minDistance = distance;
                boneIndex = i;
            }
        }

        if (boneIndex != -1)
        {
            return boneIndex;
        }
        else
        {
            Debug.LogWarning("Failed to find matching bone index for transform: " + boneTransform.name);
            return -1; // or another appropriate default value
        }
    }


}
