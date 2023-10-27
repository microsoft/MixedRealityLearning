// Copyright (c) Microsoft Corporation. 
// Licensed under the MIT License.

using MixedReality.Toolkit.SpatialManipulation;
using UnityEngine;

namespace MRTK.Tutorials.AzureCloudServices.Scripts.UX
{
    /// <summary>
    /// Handles the anchor finder indicator.
    /// </summary>
    public class AnchorArrowGuide : MonoBehaviour
    {
        [SerializeField]
        private DirectionalIndicator directionalIndicator = default;
        
        private void OnBecameInvisible()
        {
            // Triggered when 'DirectionalIndicator' component disables the Mesh Renderer
            SetTargetObject(null);
        }

        /// <summary>
        /// Sets the target object for the indicator.
        /// Passing 'null' argument resets target and disables indicator.
        /// </summary>
        /// <param name="targetObject">The object to be targeted.</param>
        public void SetTargetObject(Transform targetObject)
        {
            if (targetObject == null)
            {
                gameObject.SetActive(false);
                directionalIndicator.DirectionalTarget = null;
                return;
            }

            var cameraTransform = Camera.main.transform;
            
            transform.position = new Vector3(cameraTransform.position.x, cameraTransform.position.y, cameraTransform.position.z + 1);
            transform.rotation = cameraTransform.rotation;
            directionalIndicator.DirectionalTarget = targetObject;
            gameObject.SetActive(true);
        }
        
    }
}
