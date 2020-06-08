using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Utilities.Solvers;

namespace MRTK.Tutorials.AzureCloudPower
{
	/// <summary>
	/// TODO: 
	/// </summary>
	[RequireComponent(typeof(TapToPlace), typeof(SphereCollider))]
	public class AnchorIndicator : MonoBehaviour
	{
		[SerializeField]
		bool allowSpeechCommand = true;
		[SerializeField]
		GameObject anchorPosition = default; // Enabled when tap to place
		[SerializeField]
		GameObject saveLocationDialog = default; // Enabled when tap to place

		AnchorManager anchorManager;
		Interactable interactableVisual;

		void OnEnable()
		{
			// Reset TapToPlace any time the game object is enabled
			ResetTapToPlace();
		}

		void Start()
		{
			// I'm enabled by my manager when I'm needed
			gameObject.SetActive(false);

			// Chache references
			anchorManager = GetComponentInParent<AnchorManager>();
			interactableVisual = GetComponentInChildren<Interactable>();

			// Set collider size to control TaptoPlace.SurfaceNormalOffset (offset = radius)
			GetComponent<SphereCollider>().radius = 0;

			// Configure Interactable
			if (allowSpeechCommand)
			{
				interactableVisual.OnClick.AddListener(OnPlacingStoppedAndOnClickHandler);
			}
			else
			{
				interactableVisual.IsEnabled = false;
			}
		}

		void ResetTapToPlace()
		{
			// Set up layer mask array
			LayerMask layerMask = LayerMask.GetMask("Spatial Awarenes");
			int layerNumber = (int)(Mathf.Log((uint)layerMask.value, 2));
			LayerMask[] layerMasks = { layerNumber };

			// Remove and add (required for AutoStart = true)
			Destroy(GetComponent<TapToPlace>());
			TapToPlace tapToPlace = gameObject.AddComponent(typeof(TapToPlace)) as TapToPlace;

			// Configure
			tapToPlace.AutoStart = true;
			tapToPlace.DefaultPlacementDistance = 1;
			tapToPlace.MaxRaycastDistance = 3;
			tapToPlace.SurfaceNormalOffset = 0;
			tapToPlace.KeepOrientationVertical = true;
			tapToPlace.RotateAccordingToSurface = false;
			tapToPlace.MagneticSurfaces = layerMasks;

			// Subscribe to event
			tapToPlace.OnPlacingStopped.AddListener(OnPlacingStoppedAndOnClickHandler);
		}

		void OnPlacingStoppedAndOnClickHandler()
		{
			Debug.Log("OnPlacingStoppedAndOnClickHandler()");

			// Enable the dialog box prompt
			saveLocationDialog.SetActive(true);

			// Disable myself because I'm no longer needed
			gameObject.SetActive(false);

			// Move the anchor position to the location of the anchor indicator and enable it
			anchorPosition.transform.position = gameObject.transform.position;
			anchorPosition.transform.rotation = gameObject.transform.rotation;
			anchorPosition.SetActive(true);
		}
	}
}
