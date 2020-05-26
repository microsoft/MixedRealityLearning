using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.UI;

namespace MRTK.Tutorials.AzureCloudPower
{
	/// <summary>
	/// TODO: 
	/// </summary>
	public class AnchorPosition : MonoBehaviour
	{
		AnchorManager anchorManager;
		Interactable interactableTipBackground;

		void Start()
		{
			// Ensure I'm disable my parent is AnchorManager (means I'm just the prefab scene template used for development)
			if (GetComponentInParent<AnchorManager>())
			{
				gameObject.SetActive(false);
			}
			else
			{
				gameObject.SetActive(true);
			}

			// Chache references
			anchorManager = FindObjectOfType<AnchorManager>();
			interactableTipBackground = GetComponentInChildren<Interactable>(true);

			// Configure Interactable
			if (interactableTipBackground != null)
			{
				interactableTipBackground.OnClick.AddListener(OnClickHandler);
			}
			else
			{
				Debug.LogError("AnchorPosition's child object TipBackground needs an Interactable component");
			}
		}

		void OnClickHandler()
		{
			// Notify manager that anchor was clicked
			anchorManager.AppManager_OnAnchorClicked();
		}
	}
}
