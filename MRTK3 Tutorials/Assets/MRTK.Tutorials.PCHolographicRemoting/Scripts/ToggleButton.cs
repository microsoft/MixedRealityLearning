// Copyright (c) Microsoft Corporation. 
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToggleButton : MonoBehaviour
{
    public GameObject ClippingObject;
    public void ToggleClipping()
    {
        ClippingObject.SetActive(!ClippingObject.activeInHierarchy);
    }
}
