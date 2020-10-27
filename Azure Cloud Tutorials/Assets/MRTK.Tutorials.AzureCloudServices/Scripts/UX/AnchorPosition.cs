using System.Collections;
using MRTK.Tutorials.AzureCloudServices.Scripts.Controller;
using MRTK.Tutorials.AzureCloudServices.Scripts.Domain;
using TMPro;
using UnityEngine;

namespace MRTK.Tutorials.AzureCloudServices.Scripts.UX
{
    /// <summary>
    /// Handles the anchor position visual.
    /// </summary>
    public class AnchorPosition : MonoBehaviour
    {
        public TrackedObject TrackedObject => trackedObject;

        [SerializeField]
        private GameObject toolTipPanel = default;
        [SerializeField]
        private ObjectCardViewController objectCard = default;
        [SerializeField]
        private TextMeshPro labelText = default;

        private TrackedObject trackedObject;

        public void Init(TrackedObject source)
        {
            toolTipPanel.SetActive(true);
            trackedObject = source;
            //Workaround because TextMeshPro label is not ready until next frame
            StartCoroutine(DelayedInitCoroutine());
        }
        
        private IEnumerator DelayedInitCoroutine()
        {
            yield return null;
            if (trackedObject != null)
            {
                labelText.text = trackedObject.Name;
                objectCard.Init(trackedObject);
            }
        }
    }
}
