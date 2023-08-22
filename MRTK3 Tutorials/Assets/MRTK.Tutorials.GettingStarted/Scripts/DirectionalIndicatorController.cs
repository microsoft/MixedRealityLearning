// Copyright (c) Microsoft Corporation. 
// Licensed under the MIT License.

using UnityEngine;

namespace MRTK.Tutorials.GettingStarted
{
    public class DirectionalIndicatorController : MonoBehaviour
    {
        private void OnBecameInvisible()
        {
            // Triggered when 'DirectionalIndicator' component disables the Mesh Renderer
            gameObject.SetActive(false);
        }
    }
}
