using UnityEngine;
using Microsoft.MixedReality.Toolkit.Experimental.Utilities;

namespace MRTK.Tutorials.AzureCloudPower
{
    /// <summary>
    /// Handles the anchor finder indicator.
    /// </summary>
    public class AnchorFinderIndicator : MonoBehaviour
    {
        private DirectionalIndicator directionalIndicator;
        private Transform mainCameraTransform;

        private void Start()
        {
            // Cache references
            directionalIndicator = GetComponent<DirectionalIndicator>();
            mainCameraTransform = Camera.main.transform;

            // Always start inactive
            gameObject.SetActive(false);
        }

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
        public void SetTargetObject(GameObject targetObject)
        {
            Debug.Log("__\nAnchorFinderIndicator.EnableIndicator()");

            if (targetObject == null)
            {
                gameObject.SetActive(false);
                directionalIndicator.DirectionalTarget = null;
                return;
            }

            var pos = mainCameraTransform.position;
            var trans = transform;

            trans.position = new Vector3(pos.x, pos.y, pos.z + 1);
            trans.rotation = mainCameraTransform.rotation;

            directionalIndicator.DirectionalTarget = targetObject.transform;
            gameObject.SetActive(true);
        }
    }
}
