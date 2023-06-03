using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Utilities.FigmaImporter;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using static GestureDetect;

public class GesturePlayback : MonoBehaviour
{

    public GameObject handModel;
    public Transform hand_R;
    public GestureDetect gestureDetect;

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


        if (hand_R != null)
        {
            // Calculate the forward-facing point on the palm relative to handToRecord
            Vector3 palmForwardPoint = hand_R.InverseTransformPoint(hand_R.position + hand_R.forward);

            // Store the palm forward point for reference
            defaultHandPosition = palmForwardPoint;
        }
        else
        {
            Debug.LogWarning("Hand object not found in the hand model hierarchy");
        }
    }

    // Set the initial/default reference rotations for the hand model bones
    private void InitializeDefaultModelRotations()
    {
        RecurseRotations(hand_R, defaultBoneRotations);
    }


    // Set the initial/default reference positions for the hand model bones
    private void InitializeDefaultModelPositions()
    {
        RecursePositions(hand_R, defaultBonePositions);
    }


    // Recursively set default rotations for finger bones
    public void RecurseRotations(Transform bone, Dictionary<string, Quaternion> rotations)
    {
        rotations[bone.name] = GetBoneRotation(bone);
        foreach (Transform child in bone)
        {
            RecurseRotations(child, rotations);
        }
    }

    // Recursively set default positions for finger bones
    public void RecursePositions(Transform bone, Dictionary<string, Vector3> positions)
    {
        positions[bone.name] = GetBonePosition(bone);
        foreach (Transform child in bone)
        {
            RecursePositions(child, positions);
        }
    }

    public void PlayGesture(string gestureName)
    {
        // Check if the gestureDetect.gestures dictionary is not null and contains the specified gesture
        if (gestureDetect.gestures != null && gestureDetect.gestures.ContainsKey(gestureName))
        {
            Gesture currentGesture = gestureDetect.gestures[gestureName];
            Debug.Log($"Playing gesture: {currentGesture.name}");

            // Check if it's a motion gesture
            if (currentGesture.motionData != null && currentGesture.motionData.Count > 1)
            {
                // It's a motion gesture
                Debug.Log("It's a motion gesture");
               // StartCoroutine(PlayGestureCoroutine(currentGesture.fingerData, currentGesture.motionData));
            }
            else if (currentGesture.fingerData != null)
            {
                // It's a static gesture
                Debug.Log("It's a static gesture");


                // Iterate over each finger in the gesture data
                foreach ((string fingerName, SerializedBoneData bone) in currentGesture.fingerData)
                {
                    // Find the finger transform in the hand model hierarchy
                    Transform finger = FindFingerTransform(hand_R, fingerName);

                    if (finger != null)
                    {
                        // Ensure the finger has a parent transform
                        if (finger.parent != null)
                        {
                            // Transform the bone position from world space to local space of the finger's parent
                            Vector3 localPosition = finger.parent.InverseTransformPoint(bone.position);

                            // Calculate the change in position based on the default bone position
                            Vector3 positionChange = localPosition - finger.localPosition;

                            // Set the finger's position using Lerp for smooth interpolation
                            //finger.localPosition = positionChange;
                        }
                        else
                        {
                            Debug.LogWarning("Finger '" + fingerName + "' does not have a parent transform.");
                        }

                        finger.localPosition = bone.position;
                        finger.rotation = bone.rotation;
                    }
                    else
                    {
                        Debug.LogWarning("Finger '" + fingerName + "' not found in the hand model hierarchy");
                    }
                }



            }
            else
            {
                Debug.LogWarning("Gesture data is empty or null for gesture: " + gestureName);
            }
        }
        else
        {
            if (gestureDetect.gestures == null)
            {
                Debug.LogWarning("gestureDetect.gestures dictionary is null");
            }
            else
            {
                Debug.LogWarning("Gesture not found: " + gestureName);
            }
        }
    }

   

    // Coroutine to Playback Motion gestureDetect.gestures on the hand model
    /*IEnumerator PlayGestureCoroutine(List<SerializedBoneData> bone, List<Vector3> handMotionFrames)
    {
        for (int frameIndex = 0; frameIndex < handMotionFrames.Count; frameIndex++)
        {
            Vector3 handPosition = handMotionFrames[frameIndex];

            // Update the hand position
            handModel.transform.position = handPosition;

            foreach (KeyValuePair<string, List<GestureDetect.SerializedFingerData>> fingerDataEntry in fingerDataFrames)
            {
                // Retrieve the finger name directly from the dictionary
                string fingerName = fingerDataEntry.Key;

                // Find the finger transform in the hand model
                Transform finger = handModel.transform.Find(fingerName);

                if (finger != null)
                {
                    // Retrieve the finger positions and rotations for the current frame
                    List<GestureDetect.SerializedFingerData> fingerDataFramesList = fingerDataEntry.Value;
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
*/
    #region
    // Recursive function to update nested finger bones
    /* private void UpdateNestedFingerBones(Transform parentBone, List<SerializedFingerData> joints, int dataIndex)
     {
         // Check if dataIndex is within the range of fingerDataList
         if (dataIndex < 0 || dataIndex >= joints.Count)
         {
             Debug.LogError("Invalid data index for finger bone.");
             return;
         }

         List<SerializedBoneData> boneDataList = joints[dataIndex].boneData;

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
                 // POSITION NEEDS TO BE CHECKED AGAINST PARENT BONE / MODEL ORIGIN?
                 // ROTATION NEEDS USE EULER ANGLES (wrapped correctly)

                 Vector3 parentPosition;
                 Quaternion parentRotation;

                 // Check if the parent bone is the hand_R bone
                 if (parentBone.name == "hand_R")
                 {
                     // Use the hand_R position as the reference position for the fingers
                     parentPosition = parentBone.localPosition;
                     parentRotation = parentBone.localRotation;
                 }
                 else
                 {
                     // Use the parent bone's position as the reference position
                     parentPosition = GetReferencePositionForBone(parentBone.name);
                     parentRotation = parentBone.localRotation;
                 }

                 // Retrieve the reference position for the bone
                 Vector3 referencePosition = GetReferencePositionForBone(bone.name);

                 // Get the saved position in the same coordinate space as the reference position
                 Vector3 savedPosition = referencePosition + boneData.position;

                 // Calculate the target position by subtracting the parent position from the saved position
                 Vector3 targetPosition = savedPosition - parentPosition;

                 // Update the finger bone position
                 bone.localPosition = targetPosition;

                 *//*Debug.Log("Bone: " + bone.name);
                 Debug.Log("Reference Position: " + referencePosition);
                 Debug.Log("Saved Position: " + boneData.position);
                 Debug.Log("Parent Position: " + parentPosition);
                 Debug.Log("Target Position: " + targetPosition);*//*


                 // Update the finger bone position
                 bone.position = targetPosition;

                 // Retrieve the rotation value
                 Quaternion referenceRotation = GetReferenceRotationForBone(bone.name);
                 Quaternion targetRotation = parentRotation * referenceRotation * boneData.rotation;

                 // Convert target rotation to Euler angles
                 Vector3 targetRotationEulerAngles = targetRotation.eulerAngles;

                 *//*Debug.Log("Saved Rotation: " + boneData.rotation.eulerAngles);
                 Debug.Log("Reference Rotation: " + referenceRotation.eulerAngles);
                 Debug.Log("Parent Rotation: " + parentRotation.eulerAngles);
                 Debug.Log("Target Rotation: " + targetRotation.eulerAngles);*//*

                 // Wrap the Euler angles into the range of -180 to 180 degrees
                 float xAngle = WrapAngle(targetRotationEulerAngles.x);
                 float yAngle = WrapAngle(targetRotationEulerAngles.y);
                 float zAngle = WrapAngle(targetRotationEulerAngles.z);

                 // Update the finger bone rotation with Euler angles
                 bone.eulerAngles = new Vector3(xAngle, yAngle, zAngle);
                 Debug.Log("Euler Angles: " + bone.eulerAngles.x + ", " + bone.eulerAngles.y + ", " + bone.eulerAngles.z);

                 Debug.Log("Updated Bone: " + bone.name);

                 // Check if fingerDataList has more finger data to update
                 if (dataIndex + 1 < joints.Count)
                 {
                     // Recursively update nested finger bones with the next finger data
                     UpdateNestedFingerBones(bone, joints, dataIndex + 1);
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
 */
    #endregion

    // For euler angles
    private float WrapAngle(float angle)
    {
        angle %= 360f;
        if (angle > 180f)
        {
            angle -= 360f;
        }
        return angle;
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

    // Retrieve the Rotation of a given bone (used in Start() to get default rotation of hand model bones)
    private Quaternion GetBoneRotation(Transform boneTransform)
    {

        if (boneTransform != null)
        {
            // Wrap the Euler angles into the range of -180 to 180 degrees
            Vector3 wrappedEulerAngles = new Vector3(
                WrapAngle(boneTransform.localEulerAngles.x),
                WrapAngle(boneTransform.localEulerAngles.y),
                WrapAngle(boneTransform.localEulerAngles.z)
            );

            //Debug.Log("Default Euler Angles for " + boneTransform.name + ": " + wrappedEulerAngles);
            return Quaternion.Euler(wrappedEulerAngles);
        }
        else
        {
            Debug.LogWarning("Bone not found: " + boneTransform.name);
            return Quaternion.identity;
        }
    }

    private string VectorString(Vector3 toPrint)
    {
        return $"({toPrint.x},{toPrint.y},{toPrint.z})";
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

    // Retrieve the position of a given bone (used in Start() to get default position of hand model bones)
    private Vector3 GetBonePosition(Transform boneTransform)
    {
        Vector3 localPosition = boneTransform.localPosition;
        //Debug.Log("Default Position for " + boneTransform.name + ": " + VectorString(localPosition));
        return localPosition;
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
