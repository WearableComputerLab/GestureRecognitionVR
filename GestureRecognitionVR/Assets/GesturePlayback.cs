using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.MixedReality.Toolkit.Utilities.FigmaImporter;
using UnityEngine;

public class GesturePlayback : MonoBehaviour
{
    
    public GameObject handModel;
    public GestureDetect gestureDetect;
    public Dictionary<string, Gesture> gestures;

    // Declare dictionaries to store default position/rotation values for each bone and whole hand model
    private Dictionary<string, Quaternion> defaultBoneRotations = new Dictionary<string, Quaternion>();
    private Dictionary<string, Vector3> defaultBonePositions = new Dictionary<string, Vector3>();
    private Vector3 defaultHandPosition;


    private void Start()
    {
        Debug.Log("STARTED!!");

        // Store the default position/rotation of the hand model for reference
        InitializeDefaultModelPositions();
        InitializeDefaultModelRotations();

        // Find the hand object in the hand model hierarchy
        Transform handObject = handModel.transform.Find("m_ca01_skeleton/hand_R");
        if (handObject != null)
        {
            
            // Calculate the forward-facing point on the palm relative to handToRecord
            Vector3 palmForwardPoint = handObject.InverseTransformPoint(handObject.position + handObject.forward);

            // Store the palm forward point for reference
            defaultHandPosition = palmForwardPoint;
        }
        else
        {
            Debug.LogWarning("Hand object not found in the hand model hierarchy");
        }
    }


    // Set the initial/default reference rotation for the hand model bones
    private void InitializeDefaultModelRotations()
    {
        defaultBoneRotations.Add("hand_R", Quaternion.identity);
        defaultBoneRotations.Add("thumb02_R", GetBoneRotation("m_ca01_skeleton/hand_R/Thumb/thumb02_R"));
        defaultBoneRotations.Add("thumb03_R", GetBoneRotation("m_ca01_skeleton/hand_R/Thumb/thumb02_R/thumb03_R"));
        defaultBoneRotations.Add("index01_R", GetBoneRotation("m_ca01_skeleton/hand_R/Index/index01_R"));
        defaultBoneRotations.Add("index02_R", GetBoneRotation("m_ca01_skeleton/hand_R/Index/index01_R/index02_R"));
        defaultBoneRotations.Add("index03_R", GetBoneRotation("m_ca01_skeleton/hand_R/Index/index01_R/index02_R/index03_R"));
        defaultBoneRotations.Add("middle01_R", GetBoneRotation("m_ca01_skeleton/hand_R/Middle/middle01_R"));
        defaultBoneRotations.Add("middle02_R", GetBoneRotation("m_ca01_skeleton/hand_R/Middle/middle01_R/middle02_R"));
        defaultBoneRotations.Add("middle03_R", GetBoneRotation("m_ca01_skeleton/hand_R/Middle/middle01_R/middle02_R/middle03_R"));
        defaultBoneRotations.Add("ring01_R", GetBoneRotation("m_ca01_skeleton/hand_R/Ring/ring01_R"));
        defaultBoneRotations.Add("ring02_R", GetBoneRotation("m_ca01_skeleton/hand_R/Ring/ring01_R/ring02_R"));
        defaultBoneRotations.Add("ring03_R", GetBoneRotation("m_ca01_skeleton/hand_R/Ring/ring01_R/ring02_R/ring03_R"));
        defaultBoneRotations.Add("pinky01_R", GetBoneRotation("m_ca01_skeleton/hand_R/Pinky/pinky01_R"));
        defaultBoneRotations.Add("pinky02_R", GetBoneRotation("m_ca01_skeleton/hand_R/Pinky/pinky01_R/pinky02_R"));
        defaultBoneRotations.Add("pinky03_R", GetBoneRotation("m_ca01_skeleton/hand_R/Pinky/pinky01_R/pinky02_R/pinky03_R"));

    }

    // Set the initial/default reference position for the hand model bones
    private void InitializeDefaultModelPositions()
    {
        defaultBonePositions.Add("hand_R", Vector3.zero);
        defaultBonePositions.Add("thumb02_R", GetBonePosition("m_ca01_skeleton/hand_R/Thumb/thumb02_R"));
        defaultBonePositions.Add("thumb03_R", GetBonePosition("m_ca01_skeleton/hand_R/Thumb/thumb02_R/thumb03_R"));
        defaultBonePositions.Add("index01_R", GetBonePosition("m_ca01_skeleton/hand_R/Index/index01_R"));
        defaultBonePositions.Add("index02_R", GetBonePosition("m_ca01_skeleton/hand_R/Index/index01_R/index02_R"));
        defaultBonePositions.Add("index03_R", GetBonePosition("m_ca01_skeleton/hand_R/Index/index01_R/index02_R/index03_R"));
        defaultBonePositions.Add("middle01_R", GetBonePosition("m_ca01_skeleton/hand_R/Middle/middle01_R"));
        defaultBonePositions.Add("middle02_R", GetBonePosition("m_ca01_skeleton/hand_R/Middle/middle01_R/middle02_R"));
        defaultBonePositions.Add("middle03_R", GetBonePosition("m_ca01_skeleton/hand_R/Middle/middle01_R/middle02_R/middle03_R"));
        defaultBonePositions.Add("ring01_R", GetBonePosition("m_ca01_skeleton/hand_R/Ring/ring01_R"));
        defaultBonePositions.Add("ring02_R", GetBonePosition("m_ca01_skeleton/hand_R/Ring/ring01_R/ring02_R"));
        defaultBonePositions.Add("ring03_R", GetBonePosition("m_ca01_skeleton/hand_R/Ring/ring01_R/ring02_R/ring03_R"));
        defaultBonePositions.Add("pinky01_R", GetBonePosition("m_ca01_skeleton/hand_R/Pinky/pinky01_R"));
        defaultBonePositions.Add("pinky02_R", GetBonePosition("m_ca01_skeleton/hand_R/Pinky/pinky01_R/pinky02_R"));
        defaultBonePositions.Add("pinky03_R", GetBonePosition("m_ca01_skeleton/hand_R/Pinky/pinky01_R/pinky02_R/pinky03_R"));
    }

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
                            int expectedPositions = fingerDataList.Count; // Number of finger positions

                            // Check if the number of positions matches the number of finger bones
                            if (fingerDataList.Count == expectedPositions)
                            {
                                // Recursive function to update nested finger bones
                                UpdateNestedFingerBones(finger, fingerDataList, 0);
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
            if (gestures == null)
            {
                Debug.LogWarning("Gestures dictionary is null");
            }
            else
            {
                Debug.LogWarning("Gesture not found: " + gestureName);
            }
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

    // Recursive function to update nested finger bones
    private void UpdateNestedFingerBones(Transform parentBone, List<Dictionary<string, GestureDetect.SerializedFingerData>> fingerDataList, int dataIndex)
    {
        // Check if dataIndex is within the range of fingerDataList
        if (dataIndex < 0 || dataIndex >= fingerDataList.Count)
        {
            Debug.LogError("Invalid data index for finger bone.");
            return;
        }

        Dictionary<string, GestureDetect.SerializedFingerData> fingerData = fingerDataList[dataIndex];

        // Check if fingerData dictionary is null or empty
        if (fingerData == null || fingerData.Count == 0)
        {
            Debug.LogWarning("No finger data found at index " + dataIndex);
            return;
        }

        // Check if fingerData contains boneData key
        if (!fingerData.TryGetValue("boneData", out GestureDetect.SerializedFingerData serializedFingerData))
        {
            Debug.LogWarning("Key 'boneData' not found in finger data at index " + dataIndex);
            return;
        }

        List<GestureDetect.SerializedBoneData> boneDataList = serializedFingerData.boneData;

        // Check if boneDataList is null or empty
        if (boneDataList == null || boneDataList.Count == 0)
        {
            Debug.LogWarning("No bone data found for finger at index " + dataIndex);
            return;
        }

        int numBones = Mathf.Min(parentBone.childCount, boneDataList.Count);

        for (int i = 0; i < numBones; i++)
        {
            GestureDetect.SerializedBoneData boneData = boneDataList[i];
            Debug.Log("Parent name: " + parentBone.name);

            // Get the bone name from the child transform
            string boneName = parentBone.GetChild(i).name;

            Transform bone = FindBoneTransform(parentBone, boneName);

            if (bone != null)
            {
                // Retrieve the rotation value
                Quaternion rotation = boneData.rotation;

                // Update the finger bone rotation
                Quaternion referenceRotation = GetReferenceRotationForBone(bone.name);
                Quaternion targetRotation = referenceRotation * rotation;
                bone.rotation = targetRotation;

                // Update the finger bone position
                Vector3 position = GetReferencePositionForBone(bone.name);
                Vector3 targetPosition = boneData.position;
                bone.position = position + targetPosition;

                Debug.Log("Updated Bone: " + bone.name);

                // Check if fingerDataList has more finger data to update
                if (dataIndex + 1 < fingerDataList.Count)
                {
                    // Recursively update nested finger bones with the next finger data
                    UpdateNestedFingerBones(bone, fingerDataList, dataIndex + 1);
                }
            }
            else
            {
                Debug.LogWarning("Bone '" + boneName + "' not found in the hand model hierarchy");
                Debug.Log("Parent Bone: " + parentBone.name);
                Debug.Log("Hierarchy: " + GetBoneHierarchy(parentBone));
            }
        }
    }



    // Helper function to get the hierarchy of bones
    private string GetBoneHierarchy(Transform bone)
    {
        string hierarchy = bone.name;

        while (bone.parent != null)
        {
            bone = bone.parent;
            hierarchy = bone.name + " > " + hierarchy;
        }

        return hierarchy;
    }


    // Retrieve the default rotation value for a bone
    private Quaternion GetReferenceRotationForBone(string boneName)
    {
        // Check if the bone name exists in the dictionary
        if (defaultBoneRotations.ContainsKey(boneName))
        {
            return defaultBoneRotations[boneName];
        }
        else
        {
            return Quaternion.identity; // Default rotation for other bones
        }
    }

    // Retrieve the Rotation of a given bone
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



    // Retrieve the default position value for a bone
    private Vector3 GetReferencePositionForBone(string boneName)
    {
        // Check if the bone name exists in the dictionary
        if (defaultBonePositions.ContainsKey(boneName))
        {
            return defaultBonePositions[boneName];
        }
        else
        {
            return Vector3.zero; // Default position for other bones
        }
    }

    // Retrieve the position of a given bone
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

  
    // Recursive function to find the finger transform in the hand model hierarchy
    private Transform FindFingerTransform(Transform parent, string fingerName)
    {
        foreach (Transform child in parent)
        {
            if (child.name.Equals(fingerName))
            {
                return child;
            }
            else
            {
                Transform foundChild = FindFingerTransform(child, fingerName);
                if (foundChild != null)
                    return foundChild;
            }
        }

        return null;
    }

    // Helper function to find the bone transform by name
    private Transform FindBoneTransform(Transform parent, string boneName)
    {
        Transform bone = null;

        // Iterate over each child transform of the parent
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);

            // Debug log to see the child's name
            // Debug.Log("Child name: " + child.name);

            // Check if the child's name matches the boneName
            if (child.name.Equals(boneName))
            {
                // Bone found!
                bone = child;
                break;
            }
            else
            {
                // Recursively search for the bone in the child's hierarchy
                bone = FindBoneTransform(child, boneName);

                // If bone is found, exit the loop
                if (bone != null)
                    break;
            }
        }

        return bone;
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
