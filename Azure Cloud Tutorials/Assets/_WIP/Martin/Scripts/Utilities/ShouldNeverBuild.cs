using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MRTK.Tutorials.AzureCloudPower
{
	public class ShouldNeverBuild : MonoBehaviour
	{
		void Start()
		{
#if UNITY_EDITOR
			gameObject.SetActive(true);
#else
		gameObject.SetActive(false);
#endif
		}
	}
}
