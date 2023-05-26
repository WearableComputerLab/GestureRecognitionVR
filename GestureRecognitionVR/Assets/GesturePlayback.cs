using System;
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
                    // Iterate over each finger in the gesture data
                    foreach (KeyValuePair<string, List<Vector3>> kvp in currentGesture.fingerData)
                    {
                        string fingerName = kvp.Key;
                        List<Vector3> fingerPositions = kvp.Value;
                        List<Quaternion> fingerRotations = new List<Quaternion>();

                        // Find the finger transform in the hand model hierarchy
                        Transform finger = FindFingerTransform(handObject, fingerName);

                        if (finger != null)
                        {
                            // Check if the number of positions matches the number of finger bones
                            if (fingerPositions.Count == finger.childCount)
                            {
                                // Iterate over each bone/joint in the finger
                                for (int i = 0; i < finger.childCount; i++)
                                {
                                    // Retrieve the finger rotation for the current bone/joint
                                    Quaternion rotation = GetReferenceRotationForBone((Bone)Enum.Parse(typeof(Bone), finger.GetChild(i).name));
                                    fingerRotations.Add(rotation);
                                }

                                // Check if the number of rotations matches the number of finger bones
                                if (fingerRotations.Count == finger.childCount)
                                {
                                    // Update the finger bone rotations
                                    UpdateFingerBoneRotationsRecursive(finger, fingerRotations);

                                    // Debug the finger positions and rotations
                                    Debug.Log("Finger Positions: " + string.Join(", ", fingerPositions));
                                    Debug.Log("Finger Rotations: " + string.Join(", ", fingerRotations));
                                }
                                else
                                {
                                    Debug.LogWarning("Incorrect number of finger rotations in the current gesture frame for finger '" + fingerName + "'. Expected: " + finger.childCount + ", Actual: " + fingerRotations.Count);
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
                return GetBoneRotation("m_ca01_skeleton/hand_R/Thumb/thumb02_R");
            case Bone.thumb03_R:
                return GetBoneRotation("m_ca01_skeleton/hand_R/Thumb/thumb02_R/thumb03_R");
            case Bone.index01_R:
                return GetBoneRotation("m_ca01_skeleton/hand_R/Index/index01_R");
            case Bone.index02_R:
                return GetBoneRotation("m_ca01_skeleton/hand_R/Index/index01_R/index02_R");
            case Bone.index03_R:
                return GetBoneRotation("m_ca01_skeleton/hand_R/Index/index01_R/index02_R/index03_R");
            case Bone.middle01_R:
                return GetBoneRotation("m_ca01_skeleton/hand_R/Middle/middle01_R");
            case Bone.middle02_R:
                return GetBoneRotation("m_ca01_skeleton/hand_R/Middle/middle01_R/middle02_R");
            case Bone.middle03_R:
                return GetBoneRotation("m_ca01_skeleton/hand_R/Middle/middle01_R/middle02_R/middle03_R");
            case Bone.ring01_R:
                return GetBoneRotation("m_ca01_skeleton/hand_R/Ring/ring01_R");
            case Bone.ring02_R:
                return GetBoneRotation("m_ca01_skeleton/hand_R/Ring/ring01_R/ring02_R");
            case Bone.ring03_R:
                return GetBoneRotation("m_ca01_skeleton/hand_R/Ring/ring01_R/ring02_R/ring03_R");
            case Bone.pinky01_R:
                return GetBoneRotation("m_ca01_skeleton/hand_R/Pinky/pinky01_R");
            case Bone.pinky02_R:
                return GetBoneRotation("m_ca01_skeleton/hand_R/Pinky/pinky01_R/pinky02_R");
            case Bone.pinky03_R:
                return GetBoneRotation("m_ca01_skeleton/hand_R/Pinky/pinky01_R/pinky02_R/pinky03_R");
            default:
                return Quaternion.identity; // Default rotation for other bones
        }
    }

    private Quaternion GetBoneRotation(string bonePath)
    {
        Transform boneTransform = handModel.transform.Find(bonePath);
        if (boneTransform != null)
        {
            return boneTransform.rotation;
        }
        else
        {
            Debug.LogWarning("Bone not found: " + bonePath);
            return Quaternion.identity;
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
                return GetBonePosition("m_ca01_skeleton/hand_R/Thumb/thumb02_R");
            case Bone.thumb03_R:
                return GetBonePosition("m_ca01_skeleton/hand_R/Thumb/thumb02_R/thumb03_R");
            case Bone.index01_R:
                return GetBonePosition("m_ca01_skeleton/hand_R/Index/index01_R");
            case Bone.index02_R:
                return GetBonePosition("m_ca01_skeleton/hand_R/Index/index01_R/index02_R");
            case Bone.index03_R:
                return GetBonePosition("m_ca01_skeleton/hand_R/Index/index01_R/index02_R/index03_R");
            case Bone.middle01_R:
                return GetBonePosition("m_ca01_skeleton/hand_R/Middle/middle01_R");
            case Bone.middle02_R:
                return GetBonePosition("m_ca01_skeleton/hand_R/Middle/middle01_R/middle02_R");
            case Bone.middle03_R:
                return GetBonePosition("m_ca01_skeleton/hand_R/Middle/middle01_R/middle02_R/middle03_R");
            case Bone.ring01_R:
                return GetBonePosition("m_ca01_skeleton/hand_R/Ring/ring01_R");
            case Bone.ring02_R:
                return GetBonePosition("m_ca01_skeleton/hand_R/Ring/ring01_R/ring02_R");
            case Bone.ring03_R:
                return GetBonePosition("m_ca01_skeleton/hand_R/Ring/ring01_R/ring02_R/ring03_R");
            case Bone.pinky01_R:
                return GetBonePosition("m_ca01_skeleton/hand_R/Pinky/pinky01_R");
            case Bone.pinky02_R:
                return GetBonePosition("m_ca01_skeleton/hand_R/Pinky/pinky01_R/pinky02_R");
            case Bone.pinky03_R:
                return GetBonePosition("m_ca01_skeleton/hand_R/Pinky/pinky01_R/pinky02_R/pinky03_R");
            default:
                return Vector3.zero; // Default position for other bones
        }
    }

    private Vector3 GetBonePosition(string bonePath)
    {
        Transform boneTransform = handModel.transform.Find(bonePath);
        if (boneTransform != null)
        {
            return boneTransform.position;
        }
        else
        {
            Debug.LogWarning("Bone not found: " + bonePath);
            return Vector3.zero;
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




    // Recursively update the positions of finger bones based on the default finger positions
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

                    // Convert the finger bone position to be relative to the root transform
                    bonePosition = fingerTransform.TransformPoint(bonePosition);

                    // Convert the relative position back to local space of the finger bone
                    bonePosition = child.InverseTransformPoint(bonePosition);

                    child.localPosition = bonePosition;
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


    // Recursively update the rotations of finger bones based on the default finger rotations
    private void UpdateFingerBoneRotationsRecursive(Transform bone, List<Quaternion> rotations)
    {
        if (bone == null || rotations == null || rotations.Count == 0)
        {
            return;
        }

        // Update the rotation of the current bone
        if (rotations.Count > 0)
        {
            bone.rotation = rotations[0];
            rotations.RemoveAt(0);
        }

        // Recursively update the rotations of child bones
        for (int i = 0; i < bone.childCount; i++)
        {
            UpdateFingerBoneRotationsRecursive(bone.GetChild(i), rotations);
        }
    }


    // Check if a transform represents a finger bone
    bool IsFingerBone(Transform transform)
    {
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
