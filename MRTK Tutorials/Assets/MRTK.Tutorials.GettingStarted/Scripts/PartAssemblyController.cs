using System.Collections;
using Microsoft.MixedReality.Toolkit.UI;
using UnityEngine;

namespace MRTK.Tutorials.GettingStarted
{
    public class PartAssemblyController : MonoBehaviour
    {
        [SerializeField] private Transform locationToPlace = default;

        private const float MinDistance = 0.001f;
        private const float MaxDistance = 0.1f;

        private AudioSource audioSource;
        private bool hasAudioSource;
        private bool isSnapped;
        private ObjectManipulator objectManipulator;
        private Transform originalParent;
        private Vector3 originalPosition;
        private Quaternion originalRotation;

        private void Start()
        {
            // Cache references
            var trans = transform;
            audioSource = GetComponent<AudioSource>();
            objectManipulator = GetComponent<ObjectManipulator>();
            originalParent = trans.parent;
            originalPosition = trans.localPosition;
            originalRotation = trans.localRotation;

            // Check if object has audio source
            hasAudioSource = audioSource != null;

            // Start coroutine to continuously check if the object has been placed
            if (locationToPlace != null) StartCoroutine(CheckPlacement());
        }

        public void ResetPlacement()
        {
            isSnapped = false;

            // Enable ability to manipulate object
            objectManipulator.enabled = true;

            // Reset parent and placement of object
            var trans = transform;
            trans.SetParent(originalParent);
            trans.localPosition = originalPosition;
            trans.localRotation = originalRotation;
        }


        // Coroutine to check current location and snap object into place if it's close enough to the target location
        private IEnumerator CheckPlacement()
        {
            while (true)
            {
                yield return new WaitForSeconds(0.01f);

                if (!isSnapped && Vector3.Distance(transform.position, locationToPlace.position) > MinDistance &&
                    Vector3.Distance(transform.position, locationToPlace.position) < MaxDistance)
                {
                    isSnapped = true;

                    // Disable ability to manipulate object
                    objectManipulator.enabled = false;

                    // Set parent and placement of object to target
                    var trans = transform;
                    trans.SetParent(locationToPlace.parent);
                    trans.position = locationToPlace.position;
                    trans.rotation = locationToPlace.rotation;

                    // Play audio snapping sound
                    if (hasAudioSource) audioSource.Play();
                }

                if (isSnapped && Vector3.Distance(transform.position, locationToPlace.position) > MinDistance)
                {
                    var trans = transform;
                    trans.position = locationToPlace.position;
                    trans.rotation = locationToPlace.rotation;
                }
            }
        }
    }
}
