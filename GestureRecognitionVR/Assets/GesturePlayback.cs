using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GesturePlayback : MonoBehaviour
{
    
    public GameObject handModel;
    public GestureDetect gestureDetect;
    public Dictionary<string, Gesture> gestures;


    private void Start()
    {
        // Load gestures from gesture list
        gestures = gestureDetect.gestures;
    }

    public void PlayGesture(string gestureName)
    {
        // if gesture name matches, change hand model finger positions to match gesture fingerData
        if (gestures.ContainsKey(gestureName))
        {
            Gesture currentGesture = gestures[gestureName];

            // check if gesture is motion or static
            if(currentGesture.motionData.Count > 1)
            {
                // its a motion gesture...
                StartCoroutine(PlayGestureCoroutine(currentGesture.fingerData, currentGesture.motionData));

            } else
            {
                //its a static gesture...
                for (int i = 0; i < handModel.transform.childCount; i++)
                {
                    Transform finger = handModel.transform.GetChild(i);
                    Vector3 fingerPosition = currentGesture.fingerData[0][i];
                    finger.position = new Vector3(fingerPosition.x, finger.position.y, finger.position.z);
                }
            }            
        }
    }

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
