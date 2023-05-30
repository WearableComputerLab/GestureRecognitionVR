using UnityEngine;
using System.Collections;


public abstract class Response
{
    protected abstract IEnumerator Routine();

    public abstract string Name();

    public void StartRoutine()
    {
        StateMachine.Instance.StartCoroutine(Routine());
    }
}


public class BlueCube : Response
{
    public override string Name() => "Blue Cube";

    /// <summary>
    /// When "Gesture 1" is recognised, change cube color to blue for 2 seconds, then back to red.
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

public class Sphere : Response
{
    public override string Name() => "Sphere";

    /// <summary>
    /// When "Gesture 2" is recognised, change cube to a sphere for 2 seconds, then back to cube.
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

public class BlueSphere : Response
{
    public override string Name() => "Blue Cube and Sphere";

    /// <summary>
    /// When "Gesture 3" is recognized, change cube color and change cube to sphere for 2 seconds, then back to original color and cube.
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