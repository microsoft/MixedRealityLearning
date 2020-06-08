#define DEVELOP
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.Utilities.Solvers;

namespace MRTK.Tutorials.AzureCloudPower
{
	/// <summary>
	/// TODO: 
	/// </summary>
#if !DEVELOP
	[RequireComponent(typeof(SpatialAnchorManager))]
#endif
	public class AnchorManager : MonoBehaviour
	{
		[SerializeField]
		GameObject anchorIndicator = default;
		[SerializeField]
		GameObject anchorPositionPrefab = default;

		// Called from dialog button 'Yes'
		public void StartPlacingAnchor()
		{
			Debug.Log("__\nAnchorManager.EnableAnchorIndicator()");

			// Enable AnchorIndicator (triggers TapToPlace with AutoStart = true)
			anchorIndicator.SetActive(true);
		}

		public void Create()
		{
			Debug.Log("__\nAnchorManager.Create()");

			AppManager_OnAnchorCreated();
		}

		#region TODO: TEMP APP MANAGER
		[SerializeField]
		GameObject objectCard = default;

		void AppManager_OnAnchorCreated()
		{
			Debug.Log("__\nAnchorManager.AppManager_OnAnchorCreated()");

			#region TODO: TEMP UX MANAGER
			// When the anchor is created, place a visual at the anchor position...
			Instantiate(anchorPositionPrefab, anchorIndicator.transform.position, anchorIndicator.transform.rotation);

			// ...and disable the the object card
			objectCard.SetActive(false);
			#endregion
		}

		public void AppManager_OnAnchorClicked()
		{
			Debug.Log("__\nAnchorManager.APPManager_OnAnchorClicked()");

			#region TODO: TEMP UX MANAGER
			// Enable the object card because user clicked anchor
			objectCard.SetActive(true);
			objectCard.GetComponent<RadialView>().enabled = true;
			#endregion
		}
		#endregion
	}
}
