// Copyright (c) Microsoft Corporation. 
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EyeTrackingTutorialDemo : MonoBehaviour
{
    [Tooltip("Euler angles by which the object should be rotated by.")]
    [SerializeField]
    Vector3 rotateByEulerAngles = new Vector3(0f, 1f, 0f);

    [Tooltip("Rotation speed factor.")]
    [SerializeField]
    float rotationSpeed = 0.5f;

    [Tooltip("Blip scale factor.")]
    [SerializeField]
    float blipScale = 2.0f;

    Vector3 originalScale;

    void Start()
    {
        originalScale = transform.localScale;
    }

    public void RotateTarget()
    {
        transform.eulerAngles = transform.eulerAngles + rotateByEulerAngles * rotationSpeed;
    }

    public void BlipTarget()
    {
        StartCoroutine(BlipTargetCoroutine());
    }

    IEnumerator BlipTargetCoroutine()
    {
        transform.localScale = originalScale * blipScale;
        yield return new WaitForSeconds(0.1f);
        transform.localScale = originalScale;
    }
}
