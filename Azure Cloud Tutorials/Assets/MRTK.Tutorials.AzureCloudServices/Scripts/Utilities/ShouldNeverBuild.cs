using UnityEngine;

namespace MRTK.Tutorials.AzureCloudServices.Scripts.Utilities
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
