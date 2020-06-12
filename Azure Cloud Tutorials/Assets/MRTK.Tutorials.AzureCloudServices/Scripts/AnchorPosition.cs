using System;
using System.Linq;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.UI;
using TMPro;

namespace MRTK.Tutorials.AzureCloudPower
{
    /// <summary>
    /// Handles the anchor position visual.
    /// </summary>
    public class AnchorPosition : MonoBehaviour
    {
        private string objectId = ""; // Object ID from App or Data Manager
        private Interactable interactable; // Interactable component on 'TipBackground' child object

        #region UX
        [SerializeField, Header("UX")]
        private TextMeshPro labelText = default;
        #endregion

        private void Start()
        {
            // Cache references
            anchorManager = FindObjectOfType<AnchorManager>();
            interactable = GetComponentInChildren<Interactable>(true);

            // Configure Interactable
            if (interactable != null)
            {
                interactable.OnClick.AddListener(OnClickHandler);
            }
            else
            {
                Debug.LogError("'AnchorPosition.interactable' is null");
            }

            if (transform.parent && transform.parent.name == "EditorAnchors")
            {
                // TODO: Remove if environment/editor anchors are not included with final project/assets
                // Use anchor object name if anchor is editor anchor
                objectId = string.Join("", gameObject.name.ToCharArray().Where(Char.IsDigit));
            }
            else
            {
                GetObjectIdFromAppManager();
            }
        }

        private void Update()
        {
            #region UX
            if (labelText.text.Contains(objectId)) return;

            labelText.text = "Object ID " + objectId;
            gameObject.name = labelText.text;
            #endregion
        }

        /// <summary>
        /// Requests App or Data Manager to load card info.
        /// Called when user clicks the anchor position label.
        /// </summary>
        private void OnClickHandler()
        {
            Debug.Log("__\nAnchorPosition.OnClickHandler()");

            // TODO: Replace with App or Data Manager 
            anchorManager.LoadCardInformation(objectId);
        }

        // TODO: Replace with App or Data Manager 
        #region TEMP APP MANAGER
        private AnchorManager anchorManager;

        /// <summary>
        /// Temporary function to be replaced by app or data manager.
        /// </summary>
        private void GetObjectIdFromAppManager()
        {
            Debug.Log("__\nAnchorPosition.GetObjectIdFromAppManager()");

            var fakeObjectIdFromAppManager = UnityEngine.Random.Range(1, 10);
            objectId = fakeObjectIdFromAppManager.ToString();
        }
        #endregion
    }
}
