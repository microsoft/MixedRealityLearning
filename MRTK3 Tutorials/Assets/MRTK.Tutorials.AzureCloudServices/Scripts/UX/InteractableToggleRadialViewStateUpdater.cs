// Copyright (c) Microsoft Corporation. 
// Licensed under the MIT License.

using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.SpatialManipulation;
using UnityEngine;

namespace MRTK.Tutorials.AzureCloudServices.Scripts.UX
{
    //[RequireComponent(typeof(Interactable))]
    public class InteractableToggleRadialViewStateUpdater : MonoBehaviour
    {
        
        [SerializeField]
        private RadialView target = default;

        private StatefulInteractable interactable;
        
        private void Awake()
        {
            interactable = GetComponent<StatefulInteractable>();
        }

        private void OnEnable()
        {
            interactable.ForceSetToggled(!target.enabled);
        }
        
    }
}
