using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GesturePlayback : MonoBehaviour
{
    public GameObject handModel;
    public Dictionary<string, Gesture> gestures;


    /// <summary>
    /// TODO
    /// </summary>
    private void Start()
    {
        // Load gestures from gesture list
        gestures = GestureDetect.Instance.gestures;
    }

    /// <summary>
    /// TODO
    /// </summary>
    /// <param name="gestureName"></param>
    public void PlayGesture(string gestureName)
    {
        // if gesture name matches, change hand model finger positions to match gesture fingerData
        if (gestures.TryGetValue(gestureName, out Gesture gesture))
        {
            // check if gesture is motion or static
            if (gesture.motionData.Count > 1)
            {
                // its a motion gesture...
                StartCoroutine(PlayGestureCoroutine(gesture.fingerData, gesture.motionData));
            }
            else
            {
                //its a static gesture...
                for (int i = 0; i < handModel.transform.childCount; i++)
                {
                    Transform finger = handModel.transform.GetChild(i);
                    Vector3 fingerPosition = gesture.fingerData[0][i];
                    finger.position = new Vector3(fingerPosition.x, finger.position.y, finger.position.z);
                }
            }
        }
    }

    /// <summary>
    /// TODO
    /// </summary>
    /// <param name="fingerDataFrames"></param>
    /// <param name="handMotionFrames"></param>
    /// <returns></returns>
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
