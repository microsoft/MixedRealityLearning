using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EyeTrackingTutorialDemo : MonoBehaviour
{
    #region Serialized variables

    [Tooltip("Euler angles by which the object should be rotated by.")]
    [SerializeField]
    private Vector3 RotateByEulerAngles = new Vector3(0f,1f,0f);

    [Tooltip("Rotation speed factor.")]
    [SerializeField]
    private float speed = 0.5f;

    #endregion


    /// <summary>
    /// Rotate game object based on specified rotation speed and Euler angles.
    /// </summary>
    public void RotateTarget()
    {
        transform.eulerAngles = transform.eulerAngles + RotateByEulerAngles * speed;
    }

    public void BlipTarget()
    {
        StartCoroutine(BlipTargetCoroutine());
        Debug.Log("Blip Called");
    }

    IEnumerator BlipTargetCoroutine()
    {
        transform.localScale -= new Vector3(0.2f, 0.2f, 0.2f);
        yield return new WaitForSeconds(0.1f);
        transform.localScale += new Vector3(0.2f, 0.2f, 0.2f);

    }
}
