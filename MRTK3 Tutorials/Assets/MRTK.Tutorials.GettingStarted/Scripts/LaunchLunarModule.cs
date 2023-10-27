// Copyright (c) Microsoft Corporation. 
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaunchLunarModule : MonoBehaviour
{
    public float thrust;
    Rigidbody rb;
    bool ThrustOn;

    private Vector3 originalPosition;
    private Quaternion originalRotation;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        originalPosition = transform.localPosition;
        originalRotation = transform.localRotation;
    }

    public void ResetModule()
    {
        StopThruster();
        rb.velocity = Vector3.zero;
        rb.isKinematic = true;
        transform.localPosition = originalPosition;
        transform.localRotation = originalRotation;
    }

    public void StartThruster()
    {
        StartCoroutine(Thruster());
    }

    public void StopThruster()
    {
        ThrustOn = false;
    }

    public IEnumerator Thruster()
    {
        rb.isKinematic = false;

        ThrustOn = true;

        yield return null;

        while (ThrustOn)
        {
            yield return new WaitForSeconds(0.01f);
            rb.AddForce(transform.up * thrust);
        }
    }
}
