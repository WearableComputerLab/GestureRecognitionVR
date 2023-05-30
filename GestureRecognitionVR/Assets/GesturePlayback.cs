using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
                    // Iterate over each finger in the gesture data
                    foreach (var fingerDataEntry in currentGesture.fingerData)
                    {
                        string fingerName = fingerDataEntry.Key;
                        List<Dictionary<string, GestureDetect.SerializedFingerData>> fingerDataList = fingerDataEntry.Value;

                        // Find the finger transform in the hand model hierarchy
                        Transform finger = FindFingerTransform(handObject, fingerName);

                        if (finger != null)
                        {
                            int expectedPositions = finger.childCount * 3; // Default: 3 positions per finger bone

                            // Special case for thumb (2 bones)
                            if (fingerName.Equals("Thumb"))
                            {
                                expectedPositions = finger.childCount * 2; // 2 positions for thumb bones
                            }

                            // Check if the number of positions matches the number of finger bones
                            if (fingerDataList.Count == expectedPositions)
                            {
                                // Iterate over each bone/joint in the finger
                                for (int i = 0; i < finger.childCount; i++)
                                {
                                    // Retrieve the finger data for the current finger from the finger data list
                                    if (i < fingerDataList.Count)
                                    {
                                        Dictionary<string, GestureDetect.SerializedFingerData> fingerData = fingerDataList[i];

                                        // Retrieve the bone data for the finger
                                        if (fingerData.TryGetValue("boneData", out GestureDetect.SerializedFingerData serializedFingerData))
                                        {
                                            List<GestureDetect.SerializedBoneData> boneDataList = serializedFingerData.boneData;

                                            Debug.Log("Finger Name: " + fingerName);
                                            Debug.Log("Bone Count for Finger '" + fingerName + "': " + boneDataList.Count);

                                            if (boneDataList.Count > 0)
                                            {
                                                // Access the bone data for the current bone/joint
                                                GestureDetect.SerializedBoneData boneData = boneDataList[0];

                                                // Retrieve the rotation value
                                                Quaternion rotation = boneData.rotation;

                                                // Update the finger bone rotation
                                                Quaternion referenceRotation = GetReferenceRotationForBone(finger.GetChild(i).name);
                                                Quaternion targetRotation = referenceRotation * rotation;
                                                finger.GetChild(i).rotation = targetRotation;

                                                // Update the finger bone position
                                                Vector3 position = GetReferencePositionForBone(finger.GetChild(i).name);
                                                Vector3 targetPosition = boneData.position;
                                                finger.GetChild(i).position = position + targetPosition;
                                            }
                                            else
                                            {
                                                Debug.LogWarning("No bone data found for finger '" + fingerName + "' and bone index " + i);
                                            }
                                        }
                                        else
                                        {
                                            Debug.LogWarning("Key 'boneData' not found in finger data for finger '" + fingerName + "' and bone index " + i);
                                        }
                                    }
                                    else
                                    {
                                        Debug.LogWarning("No finger data found for finger '" + fingerName + "' and bone index " + i);
                                    }
                                }
                            }
                            else
                            {
                                Debug.LogWarning("Incorrect number of finger positions for finger '" + fingerName + "'");
                            }
                        }
                        else
                        {
                            Debug.LogWarning("Finger '" + fingerName + "' not found in the hand model hierarchy");
                        }
                    }
                }
                else
                {
                    Debug.LogWarning("Hand object not found in the hand model hierarchy");
                }
            }
            else
            {
                Debug.LogWarning("Gesture data is empty or null for gesture: " + gestureName);
            }
        }
        else
        {
            Debug.LogWarning("Gesture not found: " + gestureName);
        }
    }



    // Coroutine to Playback Motion Gestures on the hand model
    IEnumerator PlayGestureCoroutine(Dictionary<string, List<Dictionary<string, GestureDetect.SerializedFingerData>>> fingerDataFrames, List<Vector3> handMotionFrames)
    {
        for (int frameIndex = 0; frameIndex < handMotionFrames.Count; frameIndex++)
        {
            Vector3 handPosition = handMotionFrames[frameIndex];

            // Update the hand position
            handModel.transform.position = handPosition;

            foreach (KeyValuePair<string, List<Dictionary<string, GestureDetect.SerializedFingerData>>> fingerDataEntry in fingerDataFrames)
            {
                // Retrieve the finger name directly from the dictionary
                string fingerName = fingerDataEntry.Key;

                // Find the finger transform in the hand model
                Transform finger = handModel.transform.Find(fingerName);

                if (finger != null)
                {
                    // Retrieve the finger positions and rotations for the current frame
                    List<Dictionary<string, GestureDetect.SerializedFingerData>> fingerDataFramesList = fingerDataEntry.Value;
                    Dictionary<string, GestureDetect.SerializedFingerData> fingerData = fingerDataFramesList[frameIndex];

                    // Not actually getting "rotation" data here, 
                    // key "rotation" is used to access the corresponding GestureDetect.SerializedFingerData object from the fingerData dictionary.
                    List<GestureDetect.SerializedBoneData> boneDataList = fingerData["rotation"].boneData;

                    // Check if the number of finger positions matches the number of finger bones
                    if (boneDataList.Count == finger.childCount)
                    {
                        // Update the finger bone positions and rotations
                        for (int boneIndex = 0; boneIndex < finger.childCount; boneIndex++)
                        {
                            Transform bone = finger.GetChild(boneIndex);
                            string boneName = bone.gameObject.name;

                            // Retrieve the bone data for the current frame
                            GestureDetect.SerializedBoneData boneData = boneDataList[boneIndex];

                            // Set the local position of the finger bone
                            bone.localPosition = boneData.position;

                            // Set the local rotation of the finger bone
                            Quaternion rotation = GetReferenceRotationForBone(boneName) * boneData.rotation;
                            bone.localRotation = rotation;
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
            }

            // Wait for a short duration before proceeding to the next frame
            yield return new WaitForSeconds(0.02f);
        }
    }




    private Quaternion GetReferenceRotationForBone(string boneName)
    {
        // Default rotation values for each bone
        switch (boneName)
        {
            case "hand_R":
                return Quaternion.identity; // Default rotation for the hand
            case "thumb02_R":
                return GetBoneRotation("m_ca01_skeleton/hand_R/Thumb/thumb02_R");
            case "thumb03_R":
                return GetBoneRotation("m_ca01_skeleton/hand_R/Thumb/thumb02_R/thumb03_R");
            case "index01_R":
                return GetBoneRotation("m_ca01_skeleton/hand_R/Index/index01_R");
            case "index02_R":
                return GetBoneRotation("m_ca01_skeleton/hand_R/Index/index01_R/index02_R");
            case "index03_R":
                return GetBoneRotation("m_ca01_skeleton/hand_R/Index/index01_R/index02_R/index03_R");
            case "middle01_R":
                return GetBoneRotation("m_ca01_skeleton/hand_R/Middle/middle01_R");
            case "middle02_R":
                return GetBoneRotation("m_ca01_skeleton/hand_R/Middle/middle01_R/middle02_R");
            case "middle03_R":
                return GetBoneRotation("m_ca01_skeleton/hand_R/Middle/middle01_R/middle02_R/middle03_R");
            case "ring01_R":
                return GetBoneRotation("m_ca01_skeleton/hand_R/Ring/ring01_R");
            case "ring02_R":
                return GetBoneRotation("m_ca01_skeleton/hand_R/Ring/ring01_R/ring02_R");
            case "ring03_R":
                return GetBoneRotation("m_ca01_skeleton/hand_R/Ring/ring01_R/ring02_R/ring03_R");
            case "pinky01_R":
                return GetBoneRotation("m_ca01_skeleton/hand_R/Pinky/pinky01_R");
            case "pinky02_R":
                return GetBoneRotation("m_ca01_skeleton/hand_R/Pinky/pinky01_R/pinky02_R");
            case "pinky03_R":
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



    private Vector3 GetReferencePositionForBone(string boneName)
    {
        // Default position values for each bone
        switch (boneName)
        {
            case "hand_R":
                return Vector3.zero; // Default position for the hand
            case "thumb02_R":
                return GetBonePosition("m_ca01_skeleton/hand_R/Thumb/thumb02_R");
            case "thumb03_R":
                return GetBonePosition("m_ca01_skeleton/hand_R/Thumb/thumb02_R/thumb03_R");
            case "index01_R":
                return GetBonePosition("m_ca01_skeleton/hand_R/Index/index01_R");
            case "index02_R":
                return GetBonePosition("m_ca01_skeleton/hand_R/Index/index01_R/index02_R");
            case "index03_R":
                return GetBonePosition("m_ca01_skeleton/hand_R/Index/index01_R/index02_R/index03_R");
            case "middle01_R":
                return GetBonePosition("m_ca01_skeleton/hand_R/Middle/middle01_R");
            case "middle02_R":
                return GetBonePosition("m_ca01_skeleton/hand_R/Middle/middle01_R/middle02_R");
            case "middle03_R":
                return GetBonePosition("m_ca01_skeleton/hand_R/Middle/middle01_R/middle02_R/middle03_R");
            case "ring01_R":
                return GetBonePosition("m_ca01_skeleton/hand_R/Ring/ring01_R");
            case "ring02_R":
                return GetBonePosition("m_ca01_skeleton/hand_R/Ring/ring01_R/ring02_R");
            case "ring03_R":
                return GetBonePosition("m_ca01_skeleton/hand_R/Ring/ring01_R/ring02_R/ring03_R");
            case "pinky01_R":
                return GetBonePosition("m_ca01_skeleton/hand_R/Pinky/pinky01_R");
            case "pinky02_R":
                return GetBonePosition("m_ca01_skeleton/hand_R/Pinky/pinky01_R/pinky02_R");
            case "pinky03_R":
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
    private void UpdateFingerBoneRotationsRecursive(Transform bone, List<Quaternion> rotations, ref int rotationIndex)
    {
        if (bone == null || rotations == null || rotationIndex >= rotations.Count)
        {
            return;
        }

        // Update the rotation of the current bone
        bone.rotation = rotations[rotationIndex];
        rotationIndex++;

        // Recursively update the rotations of child bones
        for (int i = 0; i < bone.childCount; i++)
        {
            UpdateFingerBoneRotationsRecursive(bone.GetChild(i), rotations, ref rotationIndex);
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
