using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.Toolkit;

namespace MRTK.Tutorials.GettingStarted
{
	/// <summary>
	/// If enabled, disables DiagnosticsSystem when running on Android or iOS.
	/// </summary>
	public class DisableDiagnosticsSystem : MonoBehaviour
	{
#if UNITY_ANDROID || UNITY_IOS
		[SerializeField]
		[Header("Android & iOS Settings")]
		bool disableDiagnosticsSystem = true;

		void Start()
		{
			if (disableDiagnosticsSystem)
			{
				CoreServices.DiagnosticsSystem.ShowDiagnostics = false;
			}
		}
#endif
	}
}
