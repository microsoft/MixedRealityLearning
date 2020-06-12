using System.Threading.Tasks;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.UI;

namespace MRTK.Tutorials.AzureCloudPower
{
    /// <summary>
    /// Controls the anchor creation progress indicator UX.
    /// </summary>
    public class AnchorCreationProgressIndicatorController : MonoBehaviour
    {
        [SerializeField, Header("Dynamic")]
        private bool waitingForAnchorCreation = true;
        [SerializeField, Header("UX")]
        private GameObject anchorCreationProgressIndicatorPrefab;

        private GameObject indicatorObject;
        private IProgressIndicator indicator;
        private Camera mainCamera;

        private void Start()
        {
            // Cache references
            mainCamera = Camera.main;
        }

        private async void OpenProgressIndicator()
        {
            Debug.Log("__\nAnchorCreationProgressIndicatorController.OpenProgressIndicator()");

            await indicator.OpenAsync();

            while (waitingForAnchorCreation)
            {
                indicator.Message = "Move your device to capture more environment data";
                await Task.Yield();
            }

            indicator.Message = "Location saved successfully";
            await indicator.CloseAsync();
        }

        /// <summary>
        /// Instantiates a progress indicator object and opens the indicator session.
        /// </summary>
        public void StartProgressIndicatorSession()
        {
            Debug.Log("__\nAnchorCreationProgressIndicatorController.StartProgressIndicatorSession()");

            // Ensure waiting for anchor creation when enabled
            waitingForAnchorCreation = true;

            if (indicatorObject == null)
            {
                var trans = mainCamera.transform;
                indicatorObject = Instantiate(anchorCreationProgressIndicatorPrefab, trans.position, trans.rotation);

                indicator = indicatorObject.GetComponent<IProgressIndicator>();
            }
            else
            {
                gameObject.SetActive(true);
            }

            if (indicator != null)
            {
                OpenProgressIndicator();
            }
            else
            {
                Debug.LogError("'nAnchorCreationProgressIndicatorController.indicator' is null");
            }
        }

        /// <summary>
        /// Ends the indicator session.
        /// </summary>
        public void EndProgressIndicatorSession()
        {
            Debug.Log("__\nAnchorCreationProgressIndicatorController.StartProgressIndicatorSession()");

            waitingForAnchorCreation = false;
        }
    }
}
