// Copyright (c) Microsoft Corporation. 
// Licensed under the MIT License.

using UnityEngine;
using UnityEngine.UI;

public class ConnectionLightController : MonoBehaviour
{
    public Sprite connectedLight;
    public Sprite disconnectedLight;
    public Image connectionLight;

    public void ShowConnected(bool showConnected)
    {
        if (showConnected)
        {
            connectionLight.sprite = connectedLight;
        }
        else
        {
            connectionLight.sprite = disconnectedLight;
        }
    }
}
