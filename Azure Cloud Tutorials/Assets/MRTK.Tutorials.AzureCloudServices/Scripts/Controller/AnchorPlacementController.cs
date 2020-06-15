using System;
using Microsoft.MixedReality.Toolkit.UI;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.Utilities.Solvers;

namespace MRTK.Tutorials.AzureCloudPower
{
    /// <summary>
    /// Handles the anchor indicator (and UX as needed).
    /// </summary>
    [RequireComponent(typeof(TapToPlace), typeof(SphereCollider))]
    public class AnchorPlacementController : MonoBehaviour
    {
        public event EventHandler<Transform> OnIndicatorPlaced;
        public event EventHandler OnIndicatorCanceled;
        
        [Header("References")]
        [SerializeField]
        private GameObject placingVisual = default;
        [SerializeField]
        private GameObject confirmingVisual = default;
        [SerializeField]
        private TapToPlace tapToPlace;

        [Header("UX")]
        [SerializeField]
        private GameObject saveLocationDialog = default;
        [SerializeField]
        private Interactable submitButton;
        [SerializeField]
        private Interactable cancelButton;
        
        private void Start()
        {
            // Set up layer mask array
            LayerMask layerMask = LayerMask.GetMask("Spatial Awarenes");
            var layerNumber = (int)(Mathf.Log((uint)layerMask.value, 2));
            LayerMask[] layerMasks = { layerNumber };

            // Configure
            tapToPlace.AutoStart = true;
            tapToPlace.DefaultPlacementDistance = 1;
            tapToPlace.MaxRaycastDistance = 3;
            tapToPlace.SurfaceNormalOffset = 0;
            tapToPlace.KeepOrientationVertical = true;
            tapToPlace.RotateAccordingToSurface = false;
            tapToPlace.MagneticSurfaces = layerMasks;

            // Always start in placing mode
            placingVisual.SetActive(true);
            confirmingVisual.SetActive(false);

            // Set collider size to control TapToPlace.SurfaceNormalOffset (offset = radius)
            GetComponent<SphereCollider>().radius = 0;
            
            submitButton.OnClick.AddListener(HandleOnSubmitButtonClick);
            cancelButton.OnClick.AddListener(HandleOnCancelButtonClick);
            
            // Always start inactive
            gameObject.SetActive(false);
        }

        public void StartIndicator()
        {
            // Ensure placing visual when enabled
            placingVisual.SetActive(true);
            confirmingVisual.SetActive(false);
            saveLocationDialog.SetActive(false);
            
            // Subscribe to event
            tapToPlace.StartPlacement();
            tapToPlace.OnPlacingStopped.AddListener(OnPlacingStopped);
        }
        
        private void HandleOnSubmitButtonClick()
        {
            saveLocationDialog.SetActive(false);
            OnIndicatorPlaced?.Invoke(this, transform);
        }
        
        private void HandleOnCancelButtonClick()
        {
            // Restart flow
            StartIndicator();
        }

        private void OnPlacingStopped()
        {
            tapToPlace.OnPlacingStopped.RemoveListener(OnPlacingStopped);

            // Toggle visuals
            placingVisual.SetActive(false);
            confirmingVisual.SetActive(true);
            saveLocationDialog.SetActive(true);
        }
    }
}
