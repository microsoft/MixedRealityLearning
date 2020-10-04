using Microsoft.MixedReality.Toolkit;
using System.Linq;
using Microsoft.MixedReality.Toolkit.SpatialAwareness;
using UnityEngine;

public class ToggleSpatialMap : MonoBehaviour
{
    public void ToggleSpatialMaps()
    {
        if (CoreServices.SpatialAwarenessSystem != null)
        {
            if (IsObserverRunning)
            {
                CoreServices.SpatialAwarenessSystem.SuspendObservers();
                CoreServices.SpatialAwarenessSystem.ClearObservations();
            }
            else
            {
                CoreServices.SpatialAwarenessSystem.ResumeObservers();
            }
        }
    }

    private bool IsObserverRunning
    {
        get
        {
            var providers =
                ((IMixedRealityDataProviderAccess)CoreServices.SpatialAwarenessSystem)
                .GetDataProviders<IMixedRealitySpatialAwarenessObserver>();
            return providers.FirstOrDefault()?.IsRunning == true;
        }
    }
}
