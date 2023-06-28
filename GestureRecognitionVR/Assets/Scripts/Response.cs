using UnityEngine;
using System.Collections;

/// <summary>
/// Class for handling the responses to gestures
/// </summary>
public abstract class Response
{
    /// <summary>
    /// Routine to be run when a gesture is recognised
    /// </summary>
    /// <returns></returns>
    protected abstract IEnumerator Routine();

    /// <summary>
    /// Name of the response
    /// </summary>
    /// <returns></returns>
    public abstract string Name();

    /// <summary>
    /// Runs the routine for the response
    /// </summary>
    public void StartRoutine()
    {
        MainStateMachine.Instance.StartCoroutine(Routine());
    }
}

/// <summary>
/// Response for when "Blue Cube" response gesture is recognised
/// </summary>
public class BlueCube : Response
{
    /// <summary>
    /// Set name to Blue Cube
    /// </summary>
    /// <returns>Name</returns>
    public override string Name() => "Blue Cube";

    /// <summary>
    /// When a "Blue Cube" gesture is recognised, change cube color to blue for 2 seconds, then back to red.
    /// </summary>
    /// <returns>Returns a WaitForSeconds for 2 seconds</returns>
    protected override IEnumerator Routine()
    {
        Material material = GestureDetect.Instance.cubeRenderer.material;
        material.color = GestureDetect.Instance.newColour;
        yield return new WaitForSeconds(2);
        material.color = GestureDetect.Instance.oldColour;
    }
}

/// <summary>
/// Response for when "Sphere" response gesture is recognised
/// </summary>
public class Sphere : Response
{
    /// <summary>
    /// Set name to Sphere
    /// </summary>
    /// <returns></returns>
    public override string Name() => "Sphere";

    /// <summary>
    /// When a "Sphere" gesture is recognised, change cube to a sphere for 2 seconds, then back to cube.
    /// </summary>
    /// <returns>Returns a WaitForSeconds for 2 seconds</returns>
    protected override IEnumerator Routine()
    {
        //If current gesture has name "Gesture 2", change cube to a sphere. After 2 seconds, it will change back.
        GestureDetect.Instance.cube2.SetActive(false);
        GestureDetect.Instance.sphere.SetActive(true);

        yield return new WaitForSeconds(2);

        GestureDetect.Instance.cube2.SetActive(true);
        GestureDetect.Instance.sphere.SetActive(false);
    }
}

/// <summary>
/// Response for when "Blue Cube and Sphere" response gesture is recognised
/// </summary>
public class BlueSphere : Response
{
    /// <summary>
    /// Set name to Blue Cube and Sphere
    /// </summary>
    /// <returns></returns>
    public override string Name() => "Blue Cube and Sphere";

    /// <summary>
    /// When "Blue Cube and Sphere" is recognized, change cube color and change cube to sphere for 2 seconds, then back to original color and cube.
    /// </summary>
    /// <returns>Returns a WaitForSeconds for 2 seconds</returns>
    protected override IEnumerator Routine()
    {
        //if current gesture is "Gesture 3", change cube color and change cube to sphere for 2 seconds
        GestureDetect.Instance.cubeRenderer.material.color = GestureDetect.Instance.newColour;
        GestureDetect.Instance.cube2.SetActive(false);
        GestureDetect.Instance.sphere.SetActive(true);

        yield return new WaitForSeconds(2);

        GestureDetect.Instance.cubeRenderer.material.color = GestureDetect.Instance.oldColour;
        GestureDetect.Instance.cube2.SetActive(true);
        GestureDetect.Instance.sphere.SetActive(false);
    }
}