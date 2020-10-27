using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Utilities.Solvers;
using UnityEngine;

namespace MRTK.Tutorials.AzureCloudServices.Scripts.UX
{
    [RequireComponent(typeof(Interactable))]
    public class InteractableToggleRadialViewStateUpdater : MonoBehaviour
    {
        [SerializeField]
        private RadialView target = default;

        private Interactable interactable;
        
        private void Awake()
        {
            interactable = GetComponent<Interactable>();
        }

        private void OnEnable()
        {
            interactable.IsToggled = !target.enabled;
        }
    }
}
