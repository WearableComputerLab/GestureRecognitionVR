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
        gestures = GestureDetect.gestures;
    }

    public void PlayGesture(string gestureName)
    {
        // if gesture name matches, change hand model finger positions to match gesture fingerData
        if (gestures.ContainsKey(gestureName))
        {
            Gesture currentGesture = gestures[gestureName];

            for (int i = 0; i < handModel.transform.childCount; i++)
            {
                Transform finger = handModel.transform.GetChild(i);
                Vector3 fingerPosition = currentGesture.fingerDatas[i];
                finger.position = new Vector3(fingerPosition.x, finger.position.y, finger.position.z);
            }
        }
    }
}



/* Using Coroutine for motion gestures
 * 
 * public void PlayGesture(Dictionary<string, List<Vector3>> gestures, string gestureName)
    {
        List<Vector3> frames = gestures[gestureName];
        StartCoroutine(PlayGestureCoroutine(frames));
    }

    IEnumerator PlayGestureCoroutine(List<Vector3> frames)
    {
        foreach (Vector3 fingerPositions in frames)
        {
            for (int i = 0; i < handModel.transform.childCount; i++)
            {
                Transform finger = handModel.transform.GetChild(i);
                finger.position = new Vector3(fingerPositions[i], finger.position.y, finger.position.z);
            }
            yield return new WaitForSeconds(0.02f);
        }
    }
*/

