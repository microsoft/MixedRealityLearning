#define DEVELOP
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.Utilities.Solvers;
using Microsoft.MixedReality.Toolkit.Experimental.Utilities;
using UnityEngine.Serialization;

namespace MRTK.Tutorials.AzureCloudPower
{
    /// <summary>
    /// Access point for Azure Spatial Anchors features.
    /// </summary>
#if !DEVELOP
    [RequireComponent(typeof(SpatialAnchorManager))]
#endif
    public class AnchorManager : MonoBehaviour
    {
        [SerializeField, Header("Anchor Manager")]
        private GameObject anchorPositionPrefab = default;

        private GameObject anchorIndicatorGo;
        private AnchorFinderIndicator anchorFinderIndicator;

        #region UX
        [SerializeField, Header("UX")]
        private GameObject objectCardGo = default;
        [SerializeField]
        private GameObject saveLocationDialogGo = default;

        private AnchorCreationProgressIndicatorController anchorProgressIndicatorController;
        #endregion

        private void Start()
        {
            // Cache references
            anchorFinderIndicator = GetComponentInChildren<AnchorFinderIndicator>(true);
            anchorIndicatorGo = GetComponentInChildren<AnchorCreationIndicator>(true).gameObject;

            #region UX
            // Cache references
            anchorProgressIndicatorController = GetComponent<AnchorCreationProgressIndicatorController>();
            #endregion
        }

        /// <summary>
        /// Enables 'AnchorCreationIndicator'.
        /// Called from 'ObjectCard' > 'Save Location' button when user is ready to save location.
        /// Called from 'SaveLocationDialog' > 'ButtonTwoA' button ("No" button) when user rejects the anchor preview position.
        /// Hooked up in Unity.
        /// </summary>
        public void StartPlacingAnchor()
        {
            Debug.Log("__\nAnchorManager.StartPlacingAnchor()");

            // Enable AnchorCreationIndicator (triggers TapToPlace with AutoStart = true)
            anchorIndicatorGo.SetActive(false);
            anchorIndicatorGo.SetActive(true);

            #region UX
            objectCardGo.SetActive(false);
            saveLocationDialogGo.SetActive(false);
            #endregion
        }

        /// <summary>
        /// Starts Azure Spatial Anchors create anchor process.
        /// Called from 'SaveLocationDialog' > 'ButtonTwoA' button ("Yes" button) when user confirms an anchor should be created at the anchor preview position.
        /// Hooked up in Unity.
        /// </summary>
        public void CreateAnchor()
        {
            Debug.Log("__\nAnchorManager.CreateAnchor()");

            // When the anchor is created, disable the anchor indicator and place a visual at the anchor position
            anchorIndicatorGo.SetActive(false);
            Instantiate(anchorPositionPrefab, anchorIndicatorGo.transform.position, anchorIndicatorGo.transform.rotation);
            
            #region UX
            anchorProgressIndicatorController.StartProgressIndicatorSession();

            objectCardGo.SetActive(false);
            saveLocationDialogGo.SetActive(false);
            #endregion
            
            // TODO: Call Azure Spatial Anchors create function
        }

        // TODO: Update summary when known where to hook this up 
        /// <summary>
        /// Starts Azure Spatial Anchors find anchor process.
        /// Called from 'Not-sure-where' when user is ready to find location.
        /// <param name="anchorId">Azure Spatial Anchors anchor ID of the object to find.</param>
        /// </summary>
        public void FindAnchor(string anchorId)
        {
            Debug.Log("__\nAnchorManager.FindAnchor()");
            
#if UNITY_EDITOR
            // TODO: Remove if environment/editor anchors are not included with final project/assets
            // Simulate anchor finding in editor
            anchorFinderIndicator.SetTargetObject(GetRandomEditorAnchor());
#endif
            // TODO: Call Azure Spatial Anchors find function
        }

        // TODO: Remove if environment/editor anchors are not included with final project/assets
        private GameObject GetRandomEditorAnchor()
        {
            var editorAnchorsGo = GameObject.Find("EditorAnchors");
            var editorAnchors = new List<GameObject>();
            
            foreach (Transform child in editorAnchorsGo.transform)
            {
                if (!child.gameObject.activeSelf)
                {
                    editorAnchors.Add(child.gameObject);
                }
            }

            var randomIndex = Random.Range(0, editorAnchors.Count);
            var editorAnchor = editorAnchors[randomIndex];
            editorAnchor.SetActive(true);
            
            return editorAnchor;
        }
        
        // TODO: Move to App or Data Manager 
        #region TEMP APP MANAGER
        /// <summary>
        /// Loads the object card with current object's info.
        /// Temporary function to be replaced by App or Data Manager.
        /// </summary>
        /// <param name="objectId">ID of the object to load.</param>
        public void LoadCardInformation(string objectId)
        {
            Debug.Log("__\nAppManager.LoadCardInformation()");

            Debug.Log("objectId: " + objectId);

            // TODO: Load object card with current object's info

            #region UX
            // Enable the object card UI
            objectCardGo.SetActive(true);
            objectCardGo.GetComponent<RadialView>().enabled = true;
            #endregion
        }
        #endregion
    }
}
