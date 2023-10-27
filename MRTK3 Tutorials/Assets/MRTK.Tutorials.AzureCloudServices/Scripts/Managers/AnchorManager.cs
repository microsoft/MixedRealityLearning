// Copyright (c) Microsoft Corporation. 
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.SpatialAnchors;
using Microsoft.Azure.SpatialAnchors.Unity;
using MRTK.Tutorials.AzureCloudServices.Scripts.Controller;
using MRTK.Tutorials.AzureCloudServices.Scripts.Domain;
using MRTK.Tutorials.AzureCloudServices.Scripts.Utilities;
using MRTK.Tutorials.AzureCloudServices.Scripts.UX;
using UnityEngine;

namespace MRTK.Tutorials.AzureCloudServices.Scripts.Managers
{
    /// <summary>
    /// Access point for Azure Spatial Anchors features.
    /// </summary>
    [RequireComponent(typeof(SpatialAnchorManager))]
    public class AnchorManager : MonoBehaviour
    {
        public event EventHandler<string> OnCreateAnchorSucceeded;
        public event EventHandler OnCreateAnchorFailed;
        public event EventHandler OnFindAnchorSucceeded;
        public event EventHandler OnPlaceAnchorCanceled;
        
        [Header("Anchor Manager")]
        [SerializeField]
        private SpatialAnchorManager cloudManager = default;
        [Header("Controller")]
        [SerializeField]
        private AnchorPlacementController anchorPlacementController = default;
        [SerializeField]
        private AnchorCreationController anchorCreationController = default;
        [Header("UX")]
        [SerializeField]
        private AnchorPosition anchorPositionPrefab = default;
        [SerializeField]
        private GameObject objectCardPrefab;
        [SerializeField]
        private AnchorArrowGuide anchorArrowGuide = default;

        private Dictionary<string, AnchorPosition> activeAnchors = new Dictionary<string, AnchorPosition>();
        private CloudSpatialAnchor currentCloudAnchor;
        private AnchorLocateCriteria anchorLocateCriteria;
        private CloudSpatialAnchorWatcher currentWatcher;
        private TrackedObject currentTrackedObject;

        private void Start()
        {
            // Subscribe to Azure Spatial Anchor events
            cloudManager.AnchorLocated += HandleAnchorLocated;
            cloudManager.SessionUpdated += (sender, args) =>
            {
                Debug.Log($"Spatial Anchors Status Updated to: {args.Status}");
            };
            cloudManager.LogDebug += (sender, args) =>
            {
                Debug.Log($"CloudManager Debug: {args.Message}");
            };
            
            anchorPlacementController.OnIndicatorPlaced += HandleOnAnchorPlaced;
            anchorArrowGuide.gameObject.SetActive(false);
        }

        public bool CheckIsAnchorActiveForTrackedObject(string spatialAnchorId)
        {
            return activeAnchors.ContainsKey(spatialAnchorId);
        }

        public void GuideToAnchor(string spatialAnchorId)
        {
            if (activeAnchors.ContainsKey(spatialAnchorId))
            {
                var anchor = activeAnchors[spatialAnchorId];
                anchorArrowGuide.SetTargetObject(anchor.transform);
            }
        }

        /// <summary>
        /// Enables 'AnchorCreationIndicator'.
        /// Called from 'ObjectCard' > 'Save Location' button when user is ready to save location.
        /// Called from 'SaveLocationDialog' > 'ButtonTwoA' button ("No" button) when user rejects the anchor preview position.
        /// Hooked up in Unity.
        /// </summary>
        public void StartPlacingAnchor(TrackedObject trackedObject)
        {
            if (anchorArrowGuide.gameObject.activeInHierarchy)
            {
                Debug.Log("Anchor creation is already active.");
                return;
            }

            currentTrackedObject = trackedObject;
            Debug.Log("Placing anchor position process started.");
            anchorPlacementController.gameObject.SetActive(true);
            anchorPlacementController.StartIndicator();
        }

        /// <summary>
        /// Starts Azure Spatial Anchors create anchor process.
        /// Called from 'SaveLocationDialog' > 'ButtonTwoA' button ("Yes" button) when user confirms an anchor should be created at the anchor preview position.
        /// Hooked up in Unity.
        /// </summary>
        public void CreateAnchor(Transform indicatorTransform)
        {
            Debug.Log("Anchor position has been set, saving location process started.");
            if (Application.isEditor)
            {
                CreateAsaAnchorEditor(indicatorTransform);
            }
            else
            {
                CreateAsaAnchor(indicatorTransform);
            }
        }

        /// <summary>
        /// Starts Azure Spatial Anchors find anchor process.
        /// Called from 'Not-sure-where' when user is ready to find location.
        /// <param name="anchorId">Azure Spatial Anchors anchor ID of the object to find.</param>
        /// </summary>
        public void FindAnchor(TrackedObject trackedObject)
        {
            currentTrackedObject = trackedObject;
            if (Application.isEditor)
            {
                FindAsaAnchorEditor();
            }
            else
            {
                FindAsaAnchor();
            }
        }

        private async void CreateAsaAnchorEditor(Transform indicatorTransform)
        {
            var indicator = Instantiate(anchorPositionPrefab, indicatorTransform.position, indicatorTransform.rotation);
            anchorCreationController.StartProgressIndicatorSession();
            await Task.Delay(2500);
            var mockAnchorId = Guid.NewGuid().ToString();
            currentTrackedObject.SpatialAnchorId = mockAnchorId;
            activeAnchors.Add(currentTrackedObject.SpatialAnchorId, indicator);
            AppDispatcher.Instance().Enqueue(() =>
            {
                indicator.Init(currentTrackedObject);
                OnCreateAnchorSucceeded?.Invoke(this, mockAnchorId);
                currentTrackedObject = null;
            });
        }

        private async void CreateAsaAnchor(Transform indicatorTransform)
        {
            Debug.Log("\nAnchorManager.CreateAsaAnchor()");
            anchorCreationController.StartProgressIndicatorSession();

            if (cloudManager.Session == null)
            {
                // Creates a new session if one does not exist
                Debug.Log("await cloudManager.CreateSessionAsync()");
                await cloudManager.CreateSessionAsync();
            }

            // Starts the session if not already started
            Debug.Log("await cloudManager.StartSessionAsync()");
            await cloudManager.StartSessionAsync();
            
            var anchorPositionIndicator = Instantiate(anchorPositionPrefab, indicatorTransform.position, indicatorTransform.rotation);

            // Attach a cloud-native anchor behavior to help keep cloud
            // and native anchors in sync.
            anchorPositionIndicator.gameObject.AddComponent<CloudNativeAnchor>();

            // Get the cloud-native anchor behavior
            CloudNativeAnchor cna = anchorPositionIndicator.GetComponent<CloudNativeAnchor>();

            // If the cloud portion of the anchor hasn't been created yet, create it
            if (cna.CloudAnchor == null)
            {
                await cna.NativeToCloud();
            }

            // Get the cloud portion of the anchor
            CloudSpatialAnchor localCloudAnchor = cna.CloudAnchor;

            // Set expiration (when anchor will be deleted from Azure)
            localCloudAnchor.Expiration = DateTimeOffset.Now.AddDays(7);

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

            // Save anchor to cloud
            while (!cloudManager.IsReadyForCreate)
            {
                await Task.Delay(330);
                var createProgress = cloudManager.SessionStatus.RecommendedForCreateProgress;
                UnityDispatcher.InvokeOnAppThread(() => Debug.Log($"Move your device to capture more environment data: {createProgress:0%}"));
            }
            Debug.Log("cloudManager is ready.");

            try
            {
                // Actually save
                Debug.Log("await cloudManager.CreateAnchorAsync(localCloudAnchor)");
                await cloudManager.CreateAnchorAsync(localCloudAnchor);
                Debug.Log("Anchor created!");

                // Store
                currentCloudAnchor = localCloudAnchor;

                // Success?
                var success = currentCloudAnchor != null;
                
                if (success)
                {
                    Debug.Log($"Azure anchor with ID '{currentCloudAnchor.Identifier}' created successfully");

                    // Update the current Azure anchor ID
                    Debug.Log($"Current Azure anchor ID updated to '{currentCloudAnchor.Identifier}'");

                    currentTrackedObject.SpatialAnchorId = currentCloudAnchor.Identifier;
                    activeAnchors.Add(currentTrackedObject.SpatialAnchorId, anchorPositionIndicator);
                    // Notify subscribers
                    Debug.Log("OnCreateAnchorSucceeded?.Invoke(this, currentCloudAnchor.Identifier)");
                    AppDispatcher.Instance().Enqueue(() =>
                    {
                        anchorPositionIndicator.Init(currentTrackedObject);
                        currentTrackedObject = null;
                        OnCreateAnchorSucceeded?.Invoke(this, currentCloudAnchor.Identifier);
                    });
                }
                else
                {
                    Debug.Log($"Failed to save cloud anchor with ID '{currentCloudAnchor.Identifier}' to Azure");

                    // Notify subscribers
                    AppDispatcher.Instance().Enqueue(() =>
                    {
                        currentTrackedObject = null;
                        Destroy(anchorPositionIndicator.gameObject);
                        OnCreateAnchorFailed?.Invoke(this, EventArgs.Empty);
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.Log(ex.ToString());
            }

            StopAzureSession();
        }

        private async void FindAsaAnchor()
        {
            Debug.Log("\nAnchorManager.FindAsaAnchor()");
            
            Debug.Log("\nanchorCreationController.StartProgressIndicatorSession()");
            anchorCreationController.StartProgressIndicatorSession();

            if (cloudManager.Session == null)
            {
                // Creates a new session if one does not exist
                Debug.Log("\ncloudManager.CreateSessionAsync()");
                await cloudManager.CreateSessionAsync();
            }

            // Starts the session if not already started
            Debug.Log("\ncloudManager.StartSessionAsync()");
            await cloudManager.StartSessionAsync();

            // Create list of anchor IDs to locate
            Debug.Log($"Trying to finding object {currentTrackedObject.Name} with anchor-id {currentTrackedObject.SpatialAnchorId}");
            anchorLocateCriteria = new AnchorLocateCriteria { Identifiers = new []{ currentTrackedObject.SpatialAnchorId } };

            // Start watching for Anchors
            if (cloudManager != null && cloudManager.Session != null)
            {
                Debug.Log("\ncurrentWatcher = cloudManager.Session.CreateWatcher(anchorLocateCriteria)");
                currentWatcher = cloudManager.Session.CreateWatcher(anchorLocateCriteria);
            }
            else
            {
                Debug.Log("Attempt to create watcher failed, no session exists");
                currentWatcher = null;
            }
        }

        private async void FindAsaAnchorEditor()
        {
            anchorCreationController.StartProgressIndicatorSession();
            await Task.Delay(3000);
            
            var targetPosition = Camera.main.transform.position;
            targetPosition.z += 0.5f;
            var indicator = Instantiate(anchorPositionPrefab);
            indicator.transform.position = targetPosition;
            indicator.Init(currentTrackedObject);
            anchorArrowGuide.SetTargetObject(indicator.transform);

            // Notify subscribers
            activeAnchors.Add(currentTrackedObject.SpatialAnchorId, indicator);
            AppDispatcher.Instance().Enqueue(() => OnFindAnchorSucceeded?.Invoke(this, EventArgs.Empty));
            currentTrackedObject = null;
        }

        private async void StopAzureSession()
        {
            // Reset the current session if there is one, and wait for any active queries to be stopped
            await cloudManager.ResetSessionAsync();

            // Stop any existing session
            cloudManager.StopSession();
        }

        #region EVENT HANDLERS
        private void HandleAnchorLocated(object sender, AnchorLocatedEventArgs args)
        {
            Debug.Log($"Anchor recognized as a possible Azure anchor");
            
            if (args.Status == LocateAnchorStatus.Located || args.Status == LocateAnchorStatus.AlreadyTracked)
            {
                currentCloudAnchor = args.Anchor;

                AppDispatcher.Instance().Enqueue(() =>
                {
                    Debug.Log($"Azure anchor located successfully");
                    var indicator = Instantiate(anchorPositionPrefab);

#if WINDOWS_UWP || UNITY_WSA
                // HoloLens: The position will be set based on the unityARUserAnchor that was located.
                // On HoloLens, if we do not have a cloudAnchor already, we will have already positioned the
                // object based on the passed in worldPos/worldRot and attached a new world anchor,
                // so we are ready to commit the anchor to the cloud if requested.
                // If we do have a cloudAnchor, we will use it's pointer to setup the world anchor,
                // which will position the object automatically.
                if (currentCloudAnchor != null)
                {
                    Debug.Log("Local anchor position successfully set to Azure anchor position");
                    Pose anchorPose = Pose.identity;
                    anchorPose = currentCloudAnchor.GetPose();

                    Debug.Log($"Setting object to anchor pose with position '{anchorPose.position}' and rotation '{anchorPose.rotation}'");

                    indicator.gameObject.AddComponent<CloudNativeAnchor>().CloudToNative(currentCloudAnchor);
                }

#elif UNITY_ANDROID || UNITY_IOS
                    Pose anchorPose = Pose.identity;
                    anchorPose = currentCloudAnchor.GetPose();

                    Debug.Log($"Setting object to anchor pose with position '{anchorPose.position}' and rotation '{anchorPose.rotation}'");
                    indicator.transform.position = anchorPose.position;
                    indicator.transform.rotation = anchorPose.rotation;

                    // Create a native anchor at the location of the object in question
                    indicator.gameObject.CreateNativeAnchor();
#endif

                    indicator.Init(currentTrackedObject);
                    anchorArrowGuide.SetTargetObject(indicator.transform);
                    activeAnchors.Add(currentTrackedObject.SpatialAnchorId, indicator);

                    // Notify subscribers
                    OnFindAnchorSucceeded?.Invoke(this, EventArgs.Empty);
                    currentWatcher?.Stop();
                    currentTrackedObject = null;
                });
            }
            else
            {
                Debug.Log($"Attempt to locate Anchor with ID '{args.Identifier}' failed, locate anchor status was not 'Located' but '{args.Status}'");
            }

            StopAzureSession();
        }
        
        private void HandleOnAnchorPlaced(object sender, Transform indicatorTransform)
        {
            anchorPlacementController.gameObject.SetActive(false);
            CreateAnchor(indicatorTransform);
        }

        private void HandleOnAnchorPlacementCanceled(object sender, EventArgs e)
        {
            OnPlaceAnchorCanceled?.Invoke(this, EventArgs.Empty);
            anchorPlacementController.gameObject.SetActive(false);
        }
        #endregion
    }
}