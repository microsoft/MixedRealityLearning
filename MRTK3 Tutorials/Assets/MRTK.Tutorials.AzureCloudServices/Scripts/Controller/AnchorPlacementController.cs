// Copyright (c) Microsoft Corporation. 
// Licensed under the MIT License.

using System;
using MixedReality.Toolkit.SpatialManipulation;
using MixedReality.Toolkit.UX;
using UnityEngine;

namespace MRTK.Tutorials.AzureCloudServices.Scripts.Controller
{
    /// <summary>
    /// Handles the anchor indicator (and UX as needed).
    /// </summary>
    [RequireComponent(typeof(TapToPlace), typeof(SphereCollider))]
    public class AnchorPlacementController : MonoBehaviour
    {
        public event EventHandler<Transform> OnIndicatorPlaced;
        
        [Header("References")]
        [SerializeField]
        private GameObject placingVisual = default;
        [SerializeField]
        private GameObject confirmingVisual = default;
        [SerializeField]
        private TapToPlace tapToPlace = default;

        [Header("UX")]
        [SerializeField]
        private DialogPool dialogPool = default;

        private IDialog saveLocationDialogInstance = default;
        
        private void Start()
        {
            // Always start in placing mode
            placingVisual.SetActive(true);
            confirmingVisual.SetActive(false);
            
            // Set up layer mask array
            LayerMask layerMask = LayerMask.GetMask("Spatial Awarenes");
            var layerNumber = (int)(Mathf.Log((uint)layerMask.value, 2));
            LayerMask[] layerMasks = { layerNumber };

            tapToPlace.DefaultPlacementDistance = 1;
            tapToPlace.MaxRaycastDistance = 3;
            tapToPlace.SurfaceNormalOffset = 0;
            tapToPlace.KeepOrientationVertical = true;
            tapToPlace.RotateAccordingToSurface = false;
            tapToPlace.MagneticSurfaces = layerMasks;

            // Set collider size to control TapToPlace.SurfaceNormalOffset (offset = radius)
            GetComponent<SphereCollider>().radius = 0;
            
            // Always start inactive
            gameObject.SetActive(false);
        }

        public void StartIndicator()
        {
            // Ensure placing visual when enabled
            placingVisual.SetActive(true);
            confirmingVisual.SetActive(false);
            saveLocationDialogInstance?.Dismiss();

            // Subscribe to event
            tapToPlace.StartPlacement();
            tapToPlace.OnPlacingStopped.AddListener(OnPlacingStopped);
        }
        
        private void HandleOnSubmitButtonClick(DialogButtonEventArgs args)
        {
            OnIndicatorPlaced?.Invoke(this, transform);
            saveLocationDialogInstance.Dismiss();
        }

        private void HandleOnCancelButtonClick(DialogButtonEventArgs args)
        {
            // Restart flow
            StartIndicator();

            saveLocationDialogInstance.Dismiss();
        }

        private void OnPlacingStopped()
        {
            tapToPlace.OnPlacingStopped.RemoveListener(OnPlacingStopped);

            // Toggle visuals
            placingVisual.SetActive(false);
            confirmingVisual.SetActive(true);

            saveLocationDialogInstance = dialogPool.Get()
                .SetHeader("Are you sure you want to save this object location?")
                .SetBody("This pointer location will be saved and you can retrieve this location later to find your object.")
                .SetPositive("Yes", HandleOnSubmitButtonClick)
                .SetNegative("No", HandleOnCancelButtonClick);

            saveLocationDialogInstance.Show();
        }
        
    }
}
