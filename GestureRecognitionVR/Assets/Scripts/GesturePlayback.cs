using System;
using System.Collections;
using System.Collections.Generic;
//using Microsoft.MixedReality.Toolkit.Utilities.FigmaImporter;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GesturePlayback : MonoBehaviour
{
    public static GesturePlayback Instance;

    public GameObject handModel;
    public Transform hand_R;

    // Declare dictionaries to store default position/rotation values for each bone and whole hand model
    private Dictionary<string, Quaternion> defaultBoneRotations = new Dictionary<string, Quaternion>();
    private Dictionary<string, Vector3> defaultBonePositions = new Dictionary<string, Vector3>();
    private Vector3 defaultHandPosition;
    private Quaternion defaultHandRotation;
    private String gestName;
    public TextMeshProUGUI userMessage;
    public Microsoft.MixedReality.Toolkit.UI.Interactable replayButton;

    // Ensure only one instance of GesturePlayback
    public void Awake()
    {
        if (Instance == null)
        {
            DontDestroyOnLoad(gameObject);
            Instance = this;
        }
        else
        {
            Instance.handModel = handModel;
            Instance.hand_R = hand_R;
            Destroy(this);
        }
    }

    /// <summary>
    /// Start Method:
    /// - Hides replay button at start, add listener to ReplayGesture method
    /// - Store the default position/rotation of the finger bones in model
    /// - Set the start position/rotation of the whole hand model
    /// - Calculates the forward-facing point on the palm relative to handToRecord
    /// </summary>
    private void Start()
    {
        // Hide replay button at start, add listener to ReplayGesture method
        if (replayButton != null)
        {
            replayButton.gameObject.SetActive(false);
            replayButton.OnClick.AddListener(ReplayGesture);
        }

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
    }

    /// <summary>
    /// ReplayGesture Method:
    /// - Plays the current motion gesture again
    /// <summary>
    private void ReplayGesture()
    {
        PlayGesture(gestName);
    }

    /// <summary>
    /// InitializeDefaultModelRotations Method:
    /// - Set the initial/default reference rotations for the hand model bones
    /// - Calls RecurseRotations with the parent hand bone, and defaultBoneRotations
    /// <summary>
    private void InitializeDefaultModelRotations()
    {
        RecurseRotations(hand_R, defaultBoneRotations);
    }

    /// <summary>
    /// InitializeDefaultModelPositions Method:
    /// - Set the initial/default reference positions for the hand model bones
    /// - Calls RecursePositions with the parent hand bone, and defaultBonePositions
    /// <summary>
    private void InitializeDefaultModelPositions()
    {
        RecursePositions(hand_R, defaultBonePositions);
    }

    /// <summary>
    /// RecurseRotations Method:
    /// - Recursively set default rotations for finger bones
    /// - Takes a bone (initially hand_R), and Dict of rotations (intially defaultBoneRotations)
    /// - for each child in the parent bone, we loop through and get/set that bones rotation value
    /// <summary>
    public void RecurseRotations(Transform bone, Dictionary<string, Quaternion> rotations)
    {
        rotations[bone.name] = GetBoneRotation(bone);
        foreach (Transform child in bone)
        {
            RecurseRotations(child, rotations);
        }
    }

    /// <summary>
    /// RecursePositions Method:
    /// - Recursively set default positions for finger bones
    /// - Takes a bone (initially hand_R), and Dict of positions (intially defaultBonePositions)
    /// - for each child in the parent bone, we loop through and get/set that bones position value
    /// <summary>
    public void RecursePositions(Transform bone, Dictionary<string, Vector3> positions)
    {
        positions[bone.name] = GetBonePosition(bone);
        foreach (Transform child in bone)
        {
            RecursePositions(child, positions);
        }
    }

    /// <summary>
    /// PlayGesture Method:
    /// - Is called when Next/Prev or Replay gesture button is pressed
    /// - Takes current gesture name, finds that gesture in the gestures list
    /// - Checks if its a motion or static gesture
    /// - If static, iterate over each bone in each finger and map their position/rotation to the hand model
    /// - If motion, start the PlayGesture Coroutine
    /// </summary>
    /// <param name="gestureName"></param>
    public void PlayGesture(string gestureName)
    {
        gestName = gestureName;

        if (replayButton != null)
        {
            replayButton.gameObject.SetActive(false);
        }

        userMessage.text = "Playing " + gestureName;
        // Check if the gestureDetect.gestures dictionary is not null and contains the specified gesture
        if (GestureDetect.Instance.gestures != null && GestureDetect.Instance.gestures.ContainsKey(gestureName))
        {
            handModel.SetActive(true);
            Gesture currentGesture = GestureDetect.Instance.gestures[gestureName];

            // Check if it's a motion gesture
            if (currentGesture.fingerData != null && currentGesture.fingerData.Count > 1)
            {
                // It's a motion gesture
                StartCoroutine(PlayGestureCoroutine(currentGesture.fingerData));
            }
            else if (currentGesture.fingerData != null)
            {
                // It's a static gesture
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
            if (GestureDetect.Instance.gestures == null)
            {
                Debug.LogWarning("gestureDetect.gestures dictionary is null");
            }
            else
            {
                Debug.LogWarning("Gesture not found: " + gestureName);
            }
        }
    }

    /// <summary>
    /// PlayGestureCoroutine Method:
    /// - Is called when playing back a motion gesture
    /// - Saves hand models initial position/rotation
    /// - Loops through each frame of the motion gesture
    /// - for each frame we get the position/rotation of each finger bone
    /// - Calculate the change in position and rotation relative to the finger's current position
    /// - Calls MoveFingerCoroutine to move the fingers with Lerp
    /// - Gets the current frame's HandPosition data
    /// - Calculate rotation/scale offset so hand model plays back gesture facing the user
    /// - Calculate change in position/rotation and calls MoveHandCoroutine to move hand with Lerp
    /// - Sets replay button to active so user can replay the gesture
    /// - Calls ResetHandModelCoroutine which returns hand to initial position ready for next gesture
    /// </summary>
    /// <param name="fingerData"></param>
    private IEnumerator PlayGestureCoroutine(List<Dictionary<string, SerializedBoneData>> fingerData)
    {
        Vector3 initialHandPosition = handModel.transform.position;
        Quaternion initialHandRotation = handModel.transform.rotation;

        // Get the initial HandPosition from the first frame of finger data
        SerializedBoneData initialHandPositionData = fingerData[0]["HandPosition"];
        Vector3 initialGesturePosition = initialHandPositionData.position;
        Quaternion initialGestureRotation = initialHandPositionData.rotation;

        // For each frame in the motion gesture...
        for (int frameIndex = 0; frameIndex < fingerData.Count; frameIndex++)
        {
            // Store the data for the current frame in frameData
            Dictionary<string, SerializedBoneData> frameData = fingerData[frameIndex];

            // For every bone in the frame, get the name and data
            foreach (KeyValuePair<string, SerializedBoneData> kvp in frameData)
            {
                string fingerName = kvp.Key;
                SerializedBoneData boneData = kvp.Value;

                // Ignore HandPosition as we focus on moving fingers first
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
                            Vector3 targetFingerRotation = boneData.rotation.eulerAngles;
                            Vector3 fingerRotationChange = targetFingerRotation - finger.localEulerAngles;

                            // Set the finger's position and rotation using Lerp for smooth interpolation
                            StartCoroutine(MoveFingerCoroutine(finger, fingerPositionChange, fingerRotationChange, 1f / 20f));
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

            // Calculate the interpolated hand position relative to the initial hand position
            Vector3 interpolatedHandPosition = initialHandPosition + (currentGesturePosition - initialGesturePosition);

            // Calculate the rotation offset (30, 0, -180 works well)
            Quaternion rotationOffset = Quaternion.Euler(30f, 0f, -180f);

            // Calculate the scale change relative to the hand's current scale
            Vector3 handScaleChange = new Vector3(1f, 1f, 1f) - handModel.transform.localScale;
            // Apply scale changes to the hand model
            handModel.transform.localScale += handScaleChange;

            // Calculate the position change relative to the hand's current position
            Vector3 handPositionChange = interpolatedHandPosition - handModel.transform.position;

            // Calculate the rotation change as a quaternion without applying the offset
            Quaternion handRotationChange = currentGestureRotation * Quaternion.Inverse(handModel.transform.rotation);

            // Call MoveHandCoroutine with the updated position and rotation
            StartCoroutine(MoveHandCoroutine(handModel.transform, handPositionChange, (rotationOffset * handRotationChange).eulerAngles, 1f / 20f));

            // Wait for the next frame
            yield return null;

        }

        replayButton.gameObject.SetActive(true);
        // After playing all frames, reset the hand model's position and rotation to the initial values
        StartCoroutine(ResetHandModelCoroutine(initialHandPosition, initialHandRotation));
    }

    /// <summary>
    /// MoveHandCoroutine Method:
    /// - Saves current position/rotation of the hand model
    /// - Calculates target rotation using rotationChange and initialRotation
    /// - While the gesture continues, calculate t using elapsed time and duration for Lerp
    /// - Calculate newPosition and newRotation values and set the hand model accordingly
    /// - Ensure hand model reaches final position after playback
    /// </summary>
    /// <param name="handModel"></param>
    /// <param name="positionChange"></param>
    /// <param name="rotationChange"></param>
    /// <param name="duration"></param>
    private IEnumerator MoveHandCoroutine(Transform handModel, Vector3 positionChange, Vector3 rotationChange, float duration)
    {
        float elapsedTime = 0f;
        Vector3 initialPosition = handModel.localPosition;
        Quaternion initialRotation = handModel.localRotation;
        Quaternion targetRotation = Quaternion.Euler(WrapEulerAngles(rotationChange)) * initialRotation;

        while (elapsedTime < duration)
        {
            // calculate time for Lerp
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);

            // Calculate the interpolated position
            Vector3 newPosition = initialPosition + positionChange * t;

            // Calculate the interpolated rotation using wrapped Euler angles 
            Quaternion newRotation = Quaternion.Slerp(initialRotation, targetRotation, t);
            Vector3 wrappedEulerAngles = WrapEulerAngles(newRotation.eulerAngles);

            // Set the hand model's position and rotation
            handModel.localPosition = newPosition;
            handModel.localRotation = newRotation;

            yield return null;
        }

        // Ensure the hand model reaches the final position and rotation exactly
        handModel.localPosition = initialPosition + positionChange;
        handModel.localRotation = initialRotation * Quaternion.Euler(rotationChange);
    }

    /// <summary>
    /// ResetHandModelCoroutine Method:
    /// - Coroutine called after playing back a motion gesture (PlayGestureCoroutine)
    /// - Resets the hand models position and rotation ready for the next gesture
    /// </summary>
    /// <param name="initialHandPosition"></param>
    /// <param name="initialHandRotation"></param>
    private IEnumerator ResetHandModelCoroutine(Vector3 initialHandPosition, Quaternion initialHandRotation)
    {
        // Delay for a short duration to allow any ongoing movements to complete
        yield return new WaitForSeconds(0.1f);

        // Reset the hand model's position and rotation to the initial values
        handModel.transform.position = initialHandPosition;
        handModel.transform.rotation = initialHandRotation;

    }

    /// <summary>
    /// MoveFingerCoroutine:
    /// - Saves current position/rotation of finger
    /// - Calculates target position/rotation using positionChange and rotationChange
    /// - While the gesture continues, calculate t using elapsed time and duration for Lerp
    /// - Set hand models finger localPosition and localRotation (local because its relative to the fingers parent)
    /// </summary>
    /// <param name="finger"></param>
    /// <param name="positionChange"></param>
    /// <param name="rotationChange"></param>
    /// <param name="duration"></param>
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

    /// <summary>
    /// WrapEulerAngles:
    /// - Helper method that wraps euler angles to the range of -180 to 180 degrees
    /// </summary>
    /// <param name="eulerAngles"></param>
    private Vector3 WrapEulerAngles(Vector3 eulerAngles)
    {
        return new Vector3(
            WrapAngle(eulerAngles.x),
            WrapAngle(eulerAngles.y),
            WrapAngle(eulerAngles.z)
        );
    }

    /// <summary>
    /// WrapAngle:
    /// - Helper method that takes just one angle and wraps it
    /// </summary>
    /// <param name="angle"></param>
    /// <returns></returns>
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


    /// <summary>
    /// GetBoneHierarchy:
    /// - Helper function for debugging that returns the bone hierarchy
    /// </summary>
    /// <param name="bone"></param>
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

    /// <summary>
    /// GetReferenceRotationForBone:
    /// - Helper method which retrieves the default rotation value for a specific bone
    /// </summary>
    /// <param name="boneName"></param>
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

    /// <summary>
    /// GetBoneRotation:
    /// - Retrieve the current rotation of a given bone (used in Start() to get default rotation of hand model bones)
    /// </summary>
    /// <param name="boneTransform"></param>
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

            return Quaternion.Euler(wrappedEulerAngles);
        }
        else
        {
            Debug.LogWarning("Bone not found: " + boneTransform.name);
            return Quaternion.identity;
        }
    }

    /// <summary>
    /// GetReferencePositionForBone:
    /// - Helper method which retrieves the default position value for a specific bone
    /// </summary>
    /// <param name="boneName"></param>
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

    /// <summary>
    /// GetBoneRotation:
    /// - Retrieve the current position of a given bone (used in Start() to get default position of hand model bones)
    /// </summary>
    /// <param name="boneTransform"></param>
    private Vector3 GetBonePosition(Transform boneTransform)
    {
        Vector3 localPosition = boneTransform.localPosition;
        return localPosition;
    }

    /// <summary>
    /// FindFingerTransform:
    /// - Recursive function to find the finger transform in the hand model hierarchy
    /// </summary>
    /// <param name="parent"></param>
    /// <param name="fingerName"></param>
    private Transform FindFingerTransform(Transform parent, string fingerName)
    {
        // for each child in parent bone, check if name is correct, otherwise recurse and check again
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
