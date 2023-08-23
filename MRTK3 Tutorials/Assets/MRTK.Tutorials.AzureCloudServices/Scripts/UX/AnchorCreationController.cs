// Copyright (c) Microsoft Corporation. 
// Licensed under the MIT License.

using System.Threading.Tasks;
//using MixedReality.Toolkit.UI;
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

        private Transform cameraTransform;
        private bool waitingForAnchorCreation;

        private void Start()
        {
            // Cache references
            cameraTransform = Camera.main.transform;
            
            // Subscribe to Anchor Manager events
            anchorManager.OnFindAnchorSucceeded += (sender, args) => waitingForAnchorCreation = false;
            anchorManager.OnCreateAnchorSucceeded += (sender, s) => waitingForAnchorCreation = false;
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
        }
    }
}
