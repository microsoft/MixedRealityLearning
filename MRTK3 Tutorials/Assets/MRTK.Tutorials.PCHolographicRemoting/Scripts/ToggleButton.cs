// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using UnityEngine;

public class ToggleButton : MonoBehaviour
{
    [SerializeField]
    private GameObject ClippingObject;

    public void ToggleClipping()
    {
        ClippingObject.SetActive(!ClippingObject.activeInHierarchy);
    }
}
