using Microsoft.MixedReality.Toolkit;
using UnityEngine;

namespace MRTK.Tutorials.AzureSpatialAnchors
{
    /// <summary>
    ///     If enabled, disables DiagnosticsSystem when running on Android or iOS.
    /// </summary>
    public class DisableDiagnosticsSystem : MonoBehaviour
    {
#if UNITY_ANDROID || UNITY_IOS
        [SerializeField] [Header("Android & iOS Settings")]
        private bool disableDiagnosticsSystem = true;

        private void Start()
        {
            if (disableDiagnosticsSystem) CoreServices.DiagnosticsSystem.ShowDiagnostics = false;
        }
#endif
    }
}
