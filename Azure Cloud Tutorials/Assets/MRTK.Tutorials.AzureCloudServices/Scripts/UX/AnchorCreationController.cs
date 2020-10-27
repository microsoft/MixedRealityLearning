using System.Threading.Tasks;
using Microsoft.MixedReality.Toolkit.UI;
using MRTK.Tutorials.AzureCloudServices.Scripts.Managers;
using UnityEngine;

namespace MRTK.Tutorials.AzureCloudServices.Scripts.UX
{
    /// <summary>
    /// Controls the anchor creation progress indicator UX.
    /// </summary>
    public class AnchorCreationController : MonoBehaviour
    {
        [SerializeField]
        private AnchorManager anchorManager = default;
        [SerializeField]
        private ProgressIndicatorOrbsRotator anchorCreationProgressIndicatorPrefab = default;
        
        private ProgressIndicatorOrbsRotator indicatorObjectInstance;
        private Transform cameraTransform;
        private bool waitingForAnchorCreation;

        private void Start()
        {
            // Cache references
            cameraTransform = Camera.main.transform;
            
            // Subscribe to Anchor Manager events
            anchorManager.OnFindAnchorSucceeded += (sender, args) => waitingForAnchorCreation = false;
            anchorManager.OnCreateAnchorSucceeded += (sender, s) => waitingForAnchorCreation = false;
            indicatorObjectInstance = Instantiate(anchorCreationProgressIndicatorPrefab, cameraTransform.position, cameraTransform.rotation);
            indicatorObjectInstance.gameObject.SetActive(false);
        }

        /// <summary>
        /// Instantiates a progress indicator object and opens the indicator session.
        /// </summary>
        public async void StartProgressIndicatorSession()
        {
            if (waitingForAnchorCreation)
            {
                return;
            }
            waitingForAnchorCreation = true;

            await indicatorObjectInstance.OpenAsync();
            while (waitingForAnchorCreation)
            {
                indicatorObjectInstance.Message = "Move your device to capture more environment data";
                await Task.Yield();
            }

            indicatorObjectInstance.Message = "Location saved successfully";
            await indicatorObjectInstance.CloseAsync();
        }
    }
}
