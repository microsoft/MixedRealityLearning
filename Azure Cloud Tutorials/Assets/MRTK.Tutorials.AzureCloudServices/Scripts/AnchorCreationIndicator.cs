using UnityEngine;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Utilities.Solvers;

namespace MRTK.Tutorials.AzureCloudPower
{
    /// <summary>
    /// Handles the anchor indicator (and UX as needed).
    /// </summary>
    [RequireComponent(typeof(TapToPlace), typeof(SphereCollider))]
    public class AnchorCreationIndicator : MonoBehaviour
    {
        [SerializeField, Header("Set in Inspector")]
        private bool allowSpeechCommand = true;
        [SerializeField]
        private GameObject placingVisual = default;
        [SerializeField]
        private GameObject confirmingVisual = default;

        private Interactable interactablePlacingVisual;

        #region UX
        [SerializeField, Header("UX")]
        private GameObject saveLocationDialog = default;
        #endregion

        private void OnEnable()
        {
            // Reset TapToPlace any time the game object is enabled
            ResetTapToPlace();

            // Ensure placing visual when enabled
            placingVisual.SetActive(true);
            confirmingVisual.SetActive(false);
        }

        private void Start()
        {
            // Cache references
            interactablePlacingVisual = GetComponentInChildren<Interactable>();

            // Always start inactive
            gameObject.SetActive(false);

            // Always start in placing mode
            placingVisual.SetActive(true);
            confirmingVisual.SetActive(false);

            // Set collider size to control TapToPlace.SurfaceNormalOffset (offset = radius)
            GetComponent<SphereCollider>().radius = 0;

            // Configure Interactable
            if (allowSpeechCommand)
            {
                interactablePlacingVisual.OnClick.AddListener(OnPlacingStopped);
            }
            else
            {
                interactablePlacingVisual.IsEnabled = false;
            }
        }

        private void ResetTapToPlace()
        {
            Debug.Log("__\nAnchorCreationIndicator.ResetTapToPlace()");

            // Set up layer mask array
            LayerMask layerMask = LayerMask.GetMask("Spatial Awarenes");
            var layerNumber = (int)(Mathf.Log((uint)layerMask.value, 2));
            LayerMask[] layerMasks = { layerNumber };

            // Remove and add (required for AutoStart = true)
            Destroy(GetComponent<TapToPlace>());
            var tapToPlace = gameObject.AddComponent(typeof(TapToPlace)) as TapToPlace;

            // Configure
            tapToPlace.AutoStart = true;
            tapToPlace.DefaultPlacementDistance = 1;
            tapToPlace.MaxRaycastDistance = 3;
            tapToPlace.SurfaceNormalOffset = 0;
            tapToPlace.KeepOrientationVertical = true;
            tapToPlace.RotateAccordingToSurface = false;
            tapToPlace.MagneticSurfaces = layerMasks;

            // Subscribe to event
            tapToPlace.OnPlacingStopped.AddListener(OnPlacingStopped);
        }

        private void OnPlacingStopped()
        {
            Debug.Log("__\nAnchorCreationIndicator.OnPlacingStopped()");

            // Toggle visuals
            placingVisual.SetActive(false);
            confirmingVisual.SetActive(true);

            #region UX
            saveLocationDialog.SetActive(true);
            #endregion
        }
    }
}
