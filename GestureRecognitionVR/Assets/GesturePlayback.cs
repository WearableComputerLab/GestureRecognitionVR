using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.UI;
//using Microsoft.MixedReality.Toolkit.Utilities.FigmaImporter;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using static GestureDetect;
using static Unity.VisualScripting.LudiqRootObjectEditor;

public class GesturePlayback : MonoBehaviour
{

    public GameObject handModel;
    public Transform hand_R;
    public GestureDetect gestureDetect;

    // Declare dictionaries to store default position/rotation values for each bone and whole hand model
    private Dictionary<string, Quaternion> defaultBoneRotations = new Dictionary<string, Quaternion>();
    private Dictionary<string, Vector3> defaultBonePositions = new Dictionary<string, Vector3>();
    private Vector3 defaultHandPosition;
    private Quaternion defaultHandRotation;
    private String gestName;
    public TextMeshProUGUI userMessage;
    public Microsoft.MixedReality.Toolkit.UI.Interactable replayButton;


    private void Start()
    {
        //Debug.Log("STARTED!!");
        replayButton.gameObject.SetActive(false);
        // Store the default position/rotation of the finger bones in model for reference
        InitializeDefaultModelPositions();
        InitializeDefaultModelRotations();
        handModel.SetActive(false);


        // Set the start position/rotation of the whole hand model to reset between gestures
        defaultHandPosition = handModel.transform.position;
        defaultHandRotation = handModel.transform.rotation;

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

        replayButton.OnClick.AddListener(ReplayGesture);
    }


    private void ReplayGesture()
    {
        PlayGesture(gestName);
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
        gestName = gestureName;
        replayButton.gameObject.SetActive(false);
        userMessage.text = "Playing " + gestureName;
        // Check if the gestureDetect.gestures dictionary is not null and contains the specified gesture
        if (gestureDetect.gestures != null && gestureDetect.gestures.ContainsKey(gestureName))
        {
            handModel.SetActive(true);
            Gesture currentGesture = gestureDetect.gestures[gestureName];
            Debug.Log($"Playing Gesture: {currentGesture.name}");

            // Check if it's a motion gesture
            if (currentGesture.fingerData != null && currentGesture.fingerData.Count > 1)
            {
                // It's a motion gesture
                Debug.Log("It's a motion gesture");
                StartCoroutine(PlayGestureCoroutine(currentGesture.fingerData));
            }
            else if (currentGesture.fingerData != null)
            {
                // It's a static gesture
                Debug.Log("It's a static gesture");

                int frameIndex = 0;

                // Iterate over each finger in the gesture data
                foreach (Dictionary<string, SerializedBoneData> frameData in currentGesture.fingerData)
                {
                    // Iterate over each bone in the frame data
                    foreach (KeyValuePair<string, SerializedBoneData> kvp in frameData)
                    {
                        string boneName = kvp.Key;
                        SerializedBoneData boneData = kvp.Value;

                        // Find the finger transform in the hand model hierarchy
                        Transform finger = FindFingerTransform(hand_R, boneName);

                        // Ignore whole hand position while going through bones
                        if (boneName != "HandPosition")
                        {
                            if (finger != null)
                            {
                                // Ensure the finger has a parent transform
                                if (finger.parent != null)
                                {
                                    // Transform the bone position from world space to local space of the finger's parent
                                    Vector3 localPosition = finger.parent.InverseTransformPoint(boneData.position);

                                    // Calculate the change in position based on the default bone position
                                    Vector3 positionChange = localPosition - finger.localPosition;

                                    // TODO: Set the finger's position using Lerp for smooth interpolation between Gestures
                                    // finger.localPosition = positionChange;
                                }
                                else
                                {
                                    Debug.LogWarning("Finger '" + boneName + "' does not have a parent transform.");
                                }

                                finger.localPosition = boneData.position;

                                Vector3 wrappedEulerAngles = new Vector3(
                                    WrapAngle(boneData.rotation.eulerAngles.x),
                                    WrapAngle(boneData.rotation.eulerAngles.y),
                                    WrapAngle(boneData.rotation.eulerAngles.z)
                                );

                                finger.localEulerAngles = wrappedEulerAngles;
                            }
                            else
                            {
                                Debug.LogWarning("Finger '" + boneName + "' not found in the hand model hierarchy");
                            }
                        }
                    }

                    frameIndex++;
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
    private IEnumerator PlayGestureCoroutine(List<Dictionary<string, SerializedBoneData>> fingerData)
    {
        Vector3 initialHandPosition = handModel.transform.position;
        Quaternion initialHandRotation = handModel.transform.rotation;

        // Get the initial HandPosition from the first frame of finger data
        SerializedBoneData initialHandPositionData = fingerData[0]["HandPosition"];
        Vector3 initialGesturePosition = initialHandPositionData.position;

        for (int frameIndex = 0; frameIndex < fingerData.Count; frameIndex++)
        {
            Dictionary<string, SerializedBoneData> frameData = fingerData[frameIndex];

            foreach (KeyValuePair<string, SerializedBoneData> kvp in frameData)
            {
                string fingerName = kvp.Key;
                SerializedBoneData boneData = kvp.Value;

                if (fingerName != "HandPosition")
                {
                    // Find the finger transform in the hand model hierarchy
                    Transform finger = FindFingerTransform(handModel.transform, fingerName);

                    if (finger != null)
                    {
                        // Ensure the finger has a parent transform
                        if (finger.parent != null)
                        {
                            // Calculate the target position relative to the finger's parent
                            Vector3 targetFingerPosition = finger.parent.TransformPoint(boneData.position);

                            // Transform the target position from world space to local space of the finger's parent
                            Vector3 localTargetPosition = finger.parent.InverseTransformPoint(targetFingerPosition);

                            // Calculate the position change relative to the finger's current position
                            Vector3 fingerPositionChange = localTargetPosition - finger.localPosition;

                            // Calculate the target rotation relative to the finger's current rotation
                            Quaternion targetFingerRotation = Quaternion.Euler(boneData.rotation.eulerAngles);
                            Quaternion fingerRotationChange = targetFingerRotation * Quaternion.Inverse(finger.localRotation);

                            // Set the finger's position and rotation using Lerp for smooth interpolation
                            StartCoroutine(MoveFingerCoroutine(finger, fingerPositionChange, fingerRotationChange.eulerAngles, 1f / 20f));
                        }
                        else
                        {
                            Debug.LogWarning("Finger '" + fingerName + "' does not have a parent transform.");
                        }
                    }
                    else
                    {
                        Debug.LogWarning("Finger '" + fingerName + "' not found in the hand model hierarchy");
                    }
                }
            }

            // Get the current frame's HandPosition data
            SerializedBoneData currentHandPositionData = frameData["HandPosition"];
            Vector3 currentGesturePosition = currentHandPositionData.position;
            Quaternion currentGestureRotation = currentHandPositionData.rotation;

            // Calculate the interpolated hand position relative to the initial gesture position
            Vector3 interpolatedHandPosition = initialHandPosition + (currentGesturePosition - initialGesturePosition);

            // Calculate the position change relative to the hand's current position
            Vector3 handPositionChange = interpolatedHandPosition - handModel.transform.position;
            // Quaternion.Inverse determines the rotation change required to transition from the initial hand rotation to the current gesture rotation
            Quaternion handRotationChange = currentGestureRotation * Quaternion.Inverse(handModel.transform.rotation);

            // Set the hand's position and rotation using Lerp for smooth interpolation (Vector3.zero for handRotationChange until rotation issue is fixed)
            StartCoroutine(MoveHandCoroutine(handModel.transform, handPositionChange, handRotationChange.eulerAngles, 1f / 20f));

            // Wait for the next frame
            yield return null;
        }

        replayButton.gameObject.SetActive(true);
        // After playing all frames, reset the hand model's position and rotation to the initial values
        StartCoroutine(ResetHandModelCoroutine(initialHandPosition, initialHandRotation));
    }


    // TODO: Fix whole hand rotation.  
    // Coroutine for moving the whole hand model with Lerp during motion gesture playback
    private IEnumerator MoveHandCoroutine(Transform handModel, Vector3 positionChange, Vector3 rotationChange, float duration)
    {
        float elapsedTime = 0f;
        Vector3 initialPosition = handModel.localPosition;
        Quaternion initialRotation = handModel.localRotation;
        Quaternion targetRotation = Quaternion.Euler(WrapEulerAngles(rotationChange)) * initialRotation;

        // Rotate hand by 180 degrees to face user. 
        targetRotation *= Quaternion.Euler(180f, 0f, 0f); 

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);

            // Calculate the interpolated position
            Vector3 newPosition = initialPosition + positionChange * t;
            
            // Calculate the interpolated rotation using wrapped Euler angles
            Quaternion newRotation = Quaternion.Slerp(initialRotation, targetRotation, t);
            Vector3 wrappedEulerAngles = WrapEulerAngles(newRotation.eulerAngles);

            // Set the hand model's position and rotation
            handModel.localPosition = newPosition;
            handModel.localRotation = Quaternion.Euler(wrappedEulerAngles);

            yield return null;
        }

        // Ensure the hand model reaches the final position and rotation exactly
        handModel.localPosition = initialPosition + positionChange;
        handModel.localRotation = targetRotation;
    }


    // Wrap Euler angles to the range of -180 to 180 degrees
    private Vector3 WrapEulerAngles(Vector3 eulerAngles)
    {
        return new Vector3(
            WrapAngle(eulerAngles.x),
            WrapAngle(eulerAngles.y),
            WrapAngle(eulerAngles.z)
        );
    }

   
    // Coroutine called after playing back a motion gesture (PlayGestureCoroutine), which resets the hand models position
    private IEnumerator ResetHandModelCoroutine(Vector3 initialHandPosition, Quaternion initialHandRotation)
    {
        // Delay for a short duration to allow any ongoing movements to complete
        yield return new WaitForSeconds(0.1f);

        // Reset the hand model's position and rotation to the initial values
        handModel.transform.position = initialHandPosition;
        handModel.transform.rotation = initialHandRotation;

       // Debug.Log("Motion Gesture Finished");
    }



    // Coroutine for moving each individual finger with Lerp during motion gesture playback
    private IEnumerator MoveFingerCoroutine(Transform finger, Vector3 positionChange, Vector3 rotationChange, float duration)
    {
        float elapsedTime = 0f;
        Vector3 initialPosition = finger.localPosition;
        Quaternion initialRotation = finger.localRotation;
        Vector3 targetPosition = finger.localPosition + positionChange;
        Quaternion targetRotation = Quaternion.Euler(finger.localEulerAngles + rotationChange);

        // Loop until the elapsed time reaches the specified duration
        while (elapsedTime < duration)
        {
            // Increment the elapsed time by the time passed since the last frame
            elapsedTime += Time.deltaTime;

            // Calculate the interpolation factor for Lerp (t) based on the elapsed time and duration
            float t = Mathf.Clamp01(elapsedTime / duration);

            // Interpolate the finger's position and rotation between the initial position/rotation and the target position/rotation
            finger.localPosition = Vector3.Lerp(initialPosition, targetPosition, t);
            finger.localRotation = Quaternion.Lerp(initialRotation, targetRotation, t);

            // Wait for the next frame
            yield return null;
        }

        // Ensure the finger's position and rotation are set to the target position and rotation after the loop ends
        finger.localPosition = targetPosition;
        finger.localRotation = targetRotation;
    }



    // For euler angles
    private float WrapAngle(float angle)
    {
        // Make sure that we get value between (-360, 360], we cannot use here module of 180 and call it a day, because we would get wrong values
        angle %= 360;
        if (angle > 180)
        {
            // If we get number above 180 we need to move the value around to get negative between (-180, 0]
            return angle - 360;
        }
        else if (angle < -180)
        {
            // If we get a number below -180 we need to move the value around to get positive between (0, 180]
            return angle + 360;
        }
        else
        {
            // We are between (-180, 180) so we just return the value
            return angle;
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


}
