using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Microsoft.Azure.SpatialAnchors;
using Microsoft.Azure.SpatialAnchors.Unity;
using Microsoft.MixedReality.Toolkit.Utilities.Solvers;

namespace MRTK.Tutorials.AzureCloudPower
{
    /// <summary>
    /// Access point for Azure Spatial Anchors features.
    /// </summary>
    [RequireComponent(typeof(SpatialAnchorManager))]
    public class AnchorManager : MonoBehaviour
    {
        [SerializeField, Header("Anchor Manager")]
        private GameObject anchorPositionPrefab = default;

        private GameObject anchorIndicatorGo;
        private AnchorFinderIndicator anchorFinderIndicator;
        private SpatialAnchorManager cloudManager;
        private CloudSpatialAnchor currentCloudAnchor;
        private AnchorLocateCriteria anchorLocateCriteria;
        private CloudSpatialAnchorWatcher currentWatcher;
        private readonly Queue<Action> dispatchQueue = new Queue<Action>();
        private GameObject currentAnchorPositionGo;

        #region UX
        [SerializeField, Header("UX")]
        private GameObject objectCardGo = default;
        [SerializeField]
        private GameObject saveLocationDialogGo = default;

        private AnchorCreationProgressIndicatorController anchorProgressIndicatorController;
        #endregion

        #region UNITY LIFECYCLE
        private void Start()
        {
            // Cache references
            anchorFinderIndicator = GetComponentInChildren<AnchorFinderIndicator>(true);
            anchorIndicatorGo = GetComponentInChildren<AnchorCreationIndicator>(true).gameObject;
            cloudManager = GetComponent<SpatialAnchorManager>();

            // Subscribe to Azure Spatial Anchor events
            cloudManager.AnchorLocated += CloudManager_AnchorLocated;

            #region UX
            // Cache references
            anchorProgressIndicatorController = GetComponent<AnchorCreationProgressIndicatorController>();
            #endregion
        }

        private void Update()
        {
            lock (dispatchQueue)
            {
                if (dispatchQueue.Count > 0)
                {
                    dispatchQueue.Dequeue()();
                }
            }
        }
        #endregion

        #region PUBLIC PROPERTIES
        [HideInInspector]
        //public string currentAzureAnchorId = "";
        public string CurrentAzureAnchorId { get; private set; }
        #endregion

        #region PUBLIC METHODS - CREAT ANCHOR
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
            currentAnchorPositionGo = Instantiate(anchorPositionPrefab, anchorIndicatorGo.transform.position, anchorIndicatorGo.transform.rotation);
#if !UNITY_EDITOR
            CreateAsaAnchor(currentAnchorPositionGo);
#endif
            #region UX
            anchorProgressIndicatorController.StartProgressIndicatorSession();

            objectCardGo.SetActive(false);
            saveLocationDialogGo.SetActive(false);
            #endregion
        }
        #endregion

        #region PUBLIC METHODS - FIND ANCHOR
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
#else
            FindAsaAnchor(anchorId);
#endif
        }
        #endregion

        #region PUBLIC EVENTS
        public delegate void CreateAnchorDelegate();
        public event CreateAnchorDelegate OnCreateAnchorSucceeded;
        public event CreateAnchorDelegate OnCreateAnchorFailed;

        public delegate void CreateLocalAnchorDelegate();
        public event CreateLocalAnchorDelegate OnFindAnchorSucceeded;
        #endregion

        #region EVENT HANDLERS
        private void CloudManager_AnchorLocated(object sender, AnchorLocatedEventArgs args)
        {
            QueueOnUpdate(new Action(() => Debug.Log($"Anchor recognized as a possible Azure anchor")));

            if (args.Status == LocateAnchorStatus.Located || args.Status == LocateAnchorStatus.AlreadyTracked)
            {
                currentCloudAnchor = args.Anchor;

                QueueOnUpdate(() =>
                {
                    Debug.Log($"Azure anchor located successfully");
#if WINDOWS_UWP || UNITY_WSA
                    currentAnchorPositionGo.CreateNativeAnchor();

                    if (currentCloudAnchor == null) return;
                    Debug.Log("Local anchor position successfully set to Azure anchor position");

                    currentAnchorPositionGo.GetComponent<UnityEngine.XR.WSA.WorldAnchor>().SetNativeSpatialAnchorPtr(currentCloudAnchor.LocalAnchor);
#elif UNITY_ANDROID || UNITY_IOS
                    Pose anchorPose = Pose.identity;
                    anchorPose = currentCloudAnchor.GetPose();

                    Debug.Log($"Setting object to anchor pose with position '{anchorPose.position}' and rotation '{anchorPose.rotation}'");
                    currentAnchorPositionGo.transform.position = anchorPose.position;
                    currentAnchorPositionGo.transform.rotation = anchorPose.rotation;

                    // Create a native anchor at the location of the object in question
                    currentAnchorPositionGo.CreateNativeAnchor();
#endif
                });
            }
            else
            {
                QueueOnUpdate(new Action(() => Debug.Log($"Attempt to locate Anchor with ID '{args.Identifier}' failed, locate anchor status was not 'Located' but '{args.Status}'")));
            }

            // Notify subscribers
            OnFindAnchorSucceeded?.Invoke();

            StopAzureSession();
        }
        #endregion

        #region INTERNAL METHODS AND COROUTINES
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

            var randomIndex = UnityEngine.Random.Range(0, editorAnchors.Count);
            var editorAnchor = editorAnchors[randomIndex];
            editorAnchor.SetActive(true);
            
            return editorAnchor;
        }

        private void QueueOnUpdate(Action updateAction)
        {
            lock (dispatchQueue)
            {
                dispatchQueue.Enqueue(updateAction);
            }
        }

        private async void CreateAsaAnchor(GameObject anchorPositionGo)
        {
            Debug.Log("\nAnchorManager.CreateAsaAnchor()");

            if (cloudManager.Session == null)
            {
                // Creates a new session if one does not exist
                await cloudManager.CreateSessionAsync();
            }

            // Starts the session if not already started
            await cloudManager.StartSessionAsync();

            // Create native XR anchor at the location of the object
            anchorPositionGo.CreateNativeAnchor();

            // Create local cloud anchor
            var localCloudAnchor = new CloudSpatialAnchor();

            // Set the local cloud anchor's position to the native XR anchor's position
            localCloudAnchor.LocalAnchor = anchorPositionGo.FindNativeAnchor().GetPointer();

            // Check to see if we got the local XR anchor pointer
            if (localCloudAnchor.LocalAnchor == IntPtr.Zero)
            {
                Debug.Log("Didn't get the local anchor...");
                return;
            }
            else
            {
                Debug.Log("Local anchor created");
            }

            // Set expiration (when anchor will be deleted from Azure)
            localCloudAnchor.Expiration = DateTimeOffset.Now.AddDays(7);

            // Save anchor to cloud
            while (!cloudManager.IsReadyForCreate)
            {
                await Task.Delay(330);
                var createProgress = cloudManager.SessionStatus.RecommendedForCreateProgress;
                QueueOnUpdate(new Action(() => Debug.Log($"Move your device to capture more environment data: {createProgress:0%}")));
            }

            try
            {
                // Actually save
                await cloudManager.CreateAnchorAsync(localCloudAnchor);

                // Store
                currentCloudAnchor = localCloudAnchor;
                localCloudAnchor = null;

                // Success?
                var success = currentCloudAnchor != null;

                if (success)
                {
                    Debug.Log($"Azure anchor with ID '{currentCloudAnchor.Identifier}' created successfully");

                    // Update the current Azure anchor ID
                    Debug.Log($"Current Azure anchor ID updated to '{currentCloudAnchor.Identifier}'");
                    CurrentAzureAnchorId = currentCloudAnchor.Identifier;

                    // Notify subscribers
                    OnCreateAnchorSucceeded?.Invoke();
                }
                else
                {
                    Debug.Log($"Failed to save cloud anchor with ID '{CurrentAzureAnchorId}' to Azure");

                    // Notify subscribers
                    OnCreateAnchorFailed?.Invoke();
                }
            }
            catch (Exception ex)
            {
                Debug.Log(ex.ToString());
            }

            StopAzureSession();
        }

        private async void FindAsaAnchor(string anchorId)
        {
            Debug.Log("\nAnchorManager.FindAsaAnchor()");

            if (cloudManager.Session == null)
            {
                // Creates a new session if one does not exist
                await cloudManager.CreateSessionAsync();
            }

            // Starts the session if not already started
            await cloudManager.StartSessionAsync();

            // Create list of anchor IDs to locate
            var anchorsToFind = new List<string> {anchorId};

            anchorLocateCriteria = new AnchorLocateCriteria {Identifiers = anchorsToFind.ToArray()};

            // Start watching for Anchors
            if ((cloudManager != null) && (cloudManager.Session != null))
            {
                currentWatcher = cloudManager.Session.CreateWatcher(anchorLocateCriteria);
            }
            else
            {
                Debug.Log("Attempt to create watcher failed, no session exists");
                currentWatcher = null;
            }
        }

        private async void StopAzureSession()
        {
            // Reset the current session if there is one, and wait for any active queries to be stopped
            await cloudManager.ResetSessionAsync();

            // Stop any existing session
            cloudManager.StopSession();
        }
        #endregion
        
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
