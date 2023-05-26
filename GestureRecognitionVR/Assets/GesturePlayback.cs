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

    public enum Bone
    {
        hand_R,
        thumb02_R,
        thumb03_R,        
        index01_R,
        index02_R,
        index03_R,
        middle01_R,
        middle02_R,
        middle03_R,
        ring01_R,
        ring02_R,
        ring03_R,
        pinky01_R,
        pinky02_R,
        pinky03_R
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
                            List<Vector3> fingerPositions = new List<Vector3>();

                            // Find the finger transform in the hand model hierarchy
                            Transform finger = FindFingerTransform(handObject, fingerName);

                            if (finger != null)
                            {
                                // Iterate over each bone/joint in the finger
                                for (int i = 0; i < finger.childCount; i++)
                                {
                                    // Retrieve the finger position for the current bone/joint
                                    Vector3 position = kvp.Value[i];
                                    fingerPositions.Add(position);
                                }

                                // Check if the number of positions matches the number of finger bones
                                if (fingerPositions.Count == finger.childCount)
                                {
                                    // Update the finger bone positions
                                    UpdateFingerBonePositionsRecursive(finger, fingerPositions);
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


    private Quaternion GetReferenceRotationForBone(Bone bone)
    {
        // default rotation values for each bone
        switch (bone)
        {
            case Bone.hand_R:
                return Quaternion.identity; // Default rotation for the hand
            case Bone.thumb02_R:
                return Quaternion.Euler(-4.027f, 0.099f, 29.069f); // Default rotation for the thumb base
            case Bone.thumb03_R:
                return Quaternion.Euler(-27.989f, 2.397f, -2.402f); // Default rotation for the thumb joint 1
            case Bone.index01_R:
                return Quaternion.Euler(-33.045f, -8.815f, -8.61f);    // etc..
            case Bone.index02_R:
                return Quaternion.Euler(10.185f, 5.971f, 3.078f);
            case Bone.index03_R:
                return Quaternion.Euler(-13.328f, -6.805f, 4.037f);
            case Bone.middle01_R:
                return Quaternion.Euler(-27.217f, -8.263f, -5.801f);
            case Bone.middle02_R:
                return Quaternion.Euler(10.583f, -3.814f, -0.32f);
            case Bone.middle03_R:
                return Quaternion.Euler(-10.12f, 8.456f, 1.415f);
            case Bone.ring01_R:
                return Quaternion.Euler(-24.229f, -6.375f, 1.026f);
            case Bone.ring02_R:
                return Quaternion.Euler(7.688f, 1.018f, -1.604f);
            case Bone.ring03_R:
                return Quaternion.Euler(-10.905f, -0.778f, 0.565f);
            case Bone.pinky01_R:
                return Quaternion.Euler(-17.425f, -10.422f, 12.275f);
            case Bone.pinky02_R:
                return Quaternion.Euler(7.427f, 3.158f, -0.919f);
            case Bone.pinky03_R:
                return Quaternion.Euler(-15.351f, -3.769f, -0.667f);

            default:
                return Quaternion.identity; // Default rotation for other bones
        }
    }

    private Vector3 GetReferencePositionForBone(Bone bone)
    {
        // default position values for each bone
        switch (bone)
        {
            case Bone.hand_R:
                return Vector3.zero; // Default position for the hand
            case Bone.thumb02_R:
                return new Vector3(-1.396984e-11f, 0.000353353f, -2.328306e-11f); // Default position for the thumb base
            case Bone.thumb03_R:
                return new Vector3(-1.396984e-11f, 0.0003805762f, -8.731149e-11f); // Default position for the thumb joint 1
            case Bone.index01_R:
                return new Vector3(3.72529e-11f, 0.0006191823f, 6.984919e-12f);    // etc..
            case Bone.index02_R:
                return new Vector3(3.49246e-12f, 0.0003990389f, 8.149073e-12f);
            case Bone.index03_R:
                return new Vector3(3.958121e-11f, 0.000250507f, -1.222361e-11f);
            case Bone.middle01_R:
                return new Vector3(8.149073e-12f, 0.0005950172f, 1.658918e-11f);
            case Bone.middle02_R:
                return new Vector3(5.518086e-10f, 0.0004380123f, -2.176966e-09f);
            case Bone.middle03_R:
                return new Vector3(9.313226e-12f, 0.0002965813f, -1.629815e-11f);
            case Bone.ring01_R:
                return new Vector3(-5.820766e-12f, 0.000530321f, -1.018634e-11f);
            case Bone.ring02_R:
                return new Vector3(-9.022188e-12f, 0.0004294311f, -1.396984e-11f);
            case Bone.ring03_R:
                return new Vector3(-1.74623e-11f, 0.0002811306f, -1.164153e-11f);
            case Bone.pinky01_R:
                return new Vector3(-5.820766e-12f, 0.0004803261f, 1.513399e-11f);
            case Bone.pinky02_R:
                return new Vector3(8.731149e-13f, 0.0003517456f, -1.047738e-11f);
            case Bone.pinky03_R:
                return new Vector3(-1.047738e-11f, 0.0001998015f, -1.164153e-11f);

            default:
                return Vector3.zero; // Default position for other bones
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
    private int GetChildBoneCountRecursive(Transform fingerBone)
    {
        int count = 0;

        // Check if the fingerBone has children
        if (fingerBone.childCount > 0)
        {
            // Iterate through the children and count the bones
            foreach (Transform child in fingerBone)
            {
                count++;
                count += GetChildBoneCountRecursive(child); // Recursively count child bones
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
