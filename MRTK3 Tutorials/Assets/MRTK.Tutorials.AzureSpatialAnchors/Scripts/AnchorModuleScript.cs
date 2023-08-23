// Copyright (c) Microsoft Corporation. 
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Microsoft.Azure.SpatialAnchors;
using Microsoft.Azure.SpatialAnchors.Unity;
using RestSharp;

#if WINDOWS_UWP
using Windows.Storage;
#endif

public class AnchorModuleScript : MonoBehaviour
{
    // Anchor ID for anchor stored in Azure (provided by Azure) 
    public string CurrentAzureAnchorID { get; set; } = string.Empty;

    private SpatialAnchorManager cloudManager;
    private CloudSpatialAnchor currentCloudAnchor;
    private AnchorLocateCriteria anchorLocateCriteria;
    private CloudSpatialAnchorWatcher currentWatcher;

    private readonly Queue<Action> dispatchQueue = new Queue<Action>();

    #region Unity Lifecycle
    void Start()
    { 
        // Get a reference to the SpatialAnchorManager component (must be on the same gameobject)
        cloudManager = GetComponent<SpatialAnchorManager>();

        // Register for Azure Spatial Anchor events
        cloudManager.AnchorLocated += CloudManager_AnchorLocated;

        anchorLocateCriteria = new AnchorLocateCriteria();
    }

    void Update()
    {
        lock (dispatchQueue)
        {
            if (dispatchQueue.Count > 0)
            {
                dispatchQueue.Dequeue()();
            }
        }
    }

    void OnDestroy()
    {
        if (cloudManager != null && cloudManager.Session != null)
        {
            cloudManager.DestroySession();
        }

        if (currentWatcher != null)
        {
            currentWatcher.Stop();
            currentWatcher = null;
        }
    }
    #endregion

    #region Public Methods
    public async void StartAzureSession()
    {
        Debug.LogError("\nLogError AnchorModuleScript.StartAzureSession()");
        Debug.Log("\nAnchorModuleScript.StartAzureSession()");

        // Notify AnchorFeedbackScript
        OnStartASASession?.Invoke();

        Debug.Log("Starting Azure session... please wait...");
        
        // Starts the session if not already started
        await cloudManager.StartSessionAsync();

        Debug.Log("Azure session started successfully");
        Debug.Log("LogError Azure session started successfully");
    }

    public async void StopAzureSession()
    {
        Debug.Log("\nAnchorModuleScript.StopAzureSession()");

        // Notify AnchorFeedbackScript
        OnEndASASession?.Invoke();
        
        Debug.Log("Stopping Azure session... please wait...");
        
        cloudManager.DestroySession();
        
        // Resets the current session if there is one, and waits for any active queries to be stopped
        await cloudManager.ResetSessionAsync();

        Debug.Log("Azure session stopped successfully");
    }

    public async void CreateAzureAnchor(GameObject theObject)
    {
        Debug.Log("\nAnchorModuleScript.CreateAzureAnchor()");

        // Notify AnchorFeedbackScript
        OnCreateAnchorStarted?.Invoke();

        // Notify AnchorFeedbackScript
        OnCreateLocalAnchor?.Invoke();
        
        //Add and configure ASA components
        CloudNativeAnchor cloudNativeAnchor = theObject.AddComponent<CloudNativeAnchor>();
        await cloudNativeAnchor.NativeToCloud();
        CloudSpatialAnchor cloudSpatialAnchor = cloudNativeAnchor.CloudAnchor;
        cloudSpatialAnchor.Expiration = DateTimeOffset.Now.AddDays(3);
        
        // Save anchor to cloud
        while (!cloudManager.IsReadyForCreate)
        {
            await Task.Delay(330);
            float createProgress = cloudManager.SessionStatus.RecommendedForCreateProgress;
            QueueOnUpdate(new Action(() => Debug.Log($"Move your device to capture more environment data: {createProgress:0%}")));
        }

        bool success;

        try
        {
            Debug.Log("Creating Azure anchor... please wait...");

            // Actually save
            await cloudManager.CreateAnchorAsync(cloudSpatialAnchor);

            // Store
            //currentCloudAnchor = localCloudAnchor;
            //localCloudAnchor = null;
            currentCloudAnchor = cloudSpatialAnchor;
            cloudSpatialAnchor = null;
            
            // Success?
            success = currentCloudAnchor != null;

            if (success)
            {
                Debug.Log($"Azure anchor with ID '{currentCloudAnchor.Identifier}' created successfully");

                // Notify AnchorFeedbackScript
                OnCreateAnchorSucceeded?.Invoke();

                // Update the current Azure anchor ID
                Debug.Log($"Current Azure anchor ID updated to '{currentCloudAnchor.Identifier}'");
                CurrentAzureAnchorID = currentCloudAnchor.Identifier;
            }
            else
            {
                Debug.Log($"Failed to save cloud anchor with ID '{CurrentAzureAnchorID}' to Azure");

                // Notify AnchorFeedbackScript
                OnCreateAnchorFailed?.Invoke();
            }
        }
        catch (Exception ex)
        {
            Debug.Log(ex.ToString());
        }
    }

    public void RemoveLocalAnchor(GameObject theObject)
    {
        Debug.Log("\nAnchorModuleScript.RemoveLocalAnchor()");
        
        // // Notify AnchorFeedbackScript
        OnRemoveLocalAnchor?.Invoke();
        Destroy(theObject.GetComponent<CloudNativeAnchor>());
        theObject.DeleteNativeAnchor();

        if (theObject.FindNativeAnchor() == null)
        {
            Debug.Log("Local anchor deleted successfully");
        }
        else
        {
            Debug.Log("Attempt to delete local anchor failed");
        }
    }

    public void FindAzureAnchor(string id = "")
    {
        Debug.Log("\nAnchorModuleScript.FindAzureAnchor()");

        if (id != "")
        {
            CurrentAzureAnchorID = id;
        }

        // Notify AnchorFeedbackScript
        OnFindASAAnchor?.Invoke();

        // Set up list of anchor IDs to locate
        List<string> anchorsToFind = new List<string>();

        if (CurrentAzureAnchorID != "")
        {
            anchorsToFind.Add(CurrentAzureAnchorID);
        }
        else
        {
            Debug.Log("Current Azure anchor ID is empty");
            return;
        }

        anchorLocateCriteria.Identifiers = anchorsToFind.ToArray();
        
        anchorLocateCriteria.BypassCache = true;
        
        Debug.Log($"Anchor locate criteria configured to look for Azure anchor with ID '{CurrentAzureAnchorID}'");

        // Start watching for Anchors
        if ((cloudManager != null) && (cloudManager.Session != null))
        {
            currentWatcher = cloudManager.Session.CreateWatcher(anchorLocateCriteria);
            Debug.Log("Watcher created");
            Debug.Log("Looking for Azure anchor... please wait...");
        }
        else
        {
            Debug.Log("Attempt to create watcher failed, no session exists");
            currentWatcher = null;
        }  
    }

    public async void DeleteAzureAnchor()
    {
        Debug.Log("\nAnchorModuleScript.DeleteAzureAnchor()");

        // Notify AnchorFeedbackScript
        OnDeleteASAAnchor?.Invoke();

        // Delete the Azure anchor with the ID specified off the server and locally
        await cloudManager.DeleteAnchorAsync(currentCloudAnchor);
        currentCloudAnchor = null;

        Debug.Log("Azure anchor deleted successfully");
    }

    public void SaveAzureAnchorIdToDisk()
    {
        Debug.Log("\nAnchorModuleScript.SaveAzureAnchorIDToDisk()");

        string filename = "SavedAzureAnchorID.txt";
        string path = Application.persistentDataPath;

#if WINDOWS_UWP
        StorageFolder storageFolder = ApplicationData.Current.LocalFolder;
        path = storageFolder.Path.Replace('\\', '/') + "/";
#endif

        string filePath = Path.Combine(path, filename);
        File.WriteAllText(filePath, CurrentAzureAnchorID);

        Debug.Log($"Current Azure anchor ID '{CurrentAzureAnchorID}' successfully saved to path '{filePath}'");
    }
  
    public void GetAzureAnchorIdFromDisk()
    {
        Debug.Log("\nAnchorModuleScript.LoadAzureAnchorIDFromDisk()");

        string filename = "SavedAzureAnchorID.txt";
        string path = Application.persistentDataPath;

#if WINDOWS_UWP
        StorageFolder storageFolder = ApplicationData.Current.LocalFolder;
        path = storageFolder.Path.Replace('\\', '/') + "/";
#endif

        string filePath = Path.Combine(path, filename);
        CurrentAzureAnchorID = File.ReadAllText(filePath);

        Debug.Log($"Current Azure anchor ID successfully updated with saved Azure anchor ID '{CurrentAzureAnchorID}' from path '{path}'");
    }
    #endregion

    #region Event Handlers
    private void CloudManager_AnchorLocated(object sender, AnchorLocatedEventArgs args)
    {
        QueueOnUpdate(new Action(() => Debug.Log($"Anchor recognized as a possible Azure anchor")));

        if (args.Status == LocateAnchorStatus.Located ) // || args.Status == LocateAnchorStatus.AlreadyTracked)
        {
            if (args.Anchor == currentCloudAnchor)
            {
                Debug.Log("rgs.Anchor = currentCloudAnchor");
            }
            else
            {
                Pose anchorPose = Pose.identity;
                anchorPose = args.Anchor.GetPose();
                
                Debug.LogFormat("args.Anchor {0} Pose {1},{2},{3}", args.Anchor.Identifier, anchorPose.position.x,anchorPose.position.y,anchorPose.position.z);
                
                anchorPose = currentCloudAnchor.GetPose();
                Debug.LogFormat("currentCloudAnchor {0} Pose {1},{2},{3}", currentCloudAnchor.Identifier, anchorPose.position.x,anchorPose.position.y,anchorPose.position.z);
                
                
            }
            currentCloudAnchor = args.Anchor;

            QueueOnUpdate(() =>
            {
                Debug.Log($"Azure anchor located successfully");

                // Notify AnchorFeedbackScript
                OnASAAnchorLocated?.Invoke();

#if WINDOWS_UWP || UNITY_WSA
                // HoloLens: The position will be set based on the unityARUserAnchor that was located.

                // Notify AnchorFeedbackScript
                OnCreateLocalAnchor?.Invoke();

                // On HoloLens, if we do not have a cloudAnchor already, we will have already positioned the
                // object based on the passed in worldPos/worldRot and attached a new world anchor,
                // so we are ready to commit the anchor to the cloud if requested.
                // If we do have a cloudAnchor, we will use it's pointer to setup the world anchor,
                // which will position the object automatically.
                if (currentCloudAnchor != null)
                {
                    gameObject.AddComponent<CloudNativeAnchor>().CloudToNative(currentCloudAnchor);
                    
                    OnCreateLocalAnchor?.Invoke();
                }

#elif UNITY_ANDROID || UNITY_IOS
                Pose anchorPose = Pose.identity;
                anchorPose = currentCloudAnchor.GetPose();

                Debug.Log($"Setting object to anchor pose with position '{anchorPose.position}' and rotation '{anchorPose.rotation}'");
                transform.position = anchorPose.position;
                transform.rotation = anchorPose.rotation;

                // Create a native anchor at the location of the object in question
                gameObject.CreateNativeAnchor();

                // Notify AnchorFeedbackScript
                OnCreateLocalAnchor?.Invoke();

#endif
            });
        }
        else
        {
            QueueOnUpdate(new Action(() => Debug.Log($"Attempt to locate Anchor with ID '{args.Identifier}' failed, locate anchor status was not 'Located' but '{args.Status}'")));
        }
    }
    #endregion

    #region Internal Methods and Coroutines
    private void QueueOnUpdate(Action updateAction)
    {
        lock (dispatchQueue)
        {
            dispatchQueue.Enqueue(updateAction);
        }
    }
    #endregion

    #region Public Events
    public delegate void StartASASessionDelegate();
    public event StartASASessionDelegate OnStartASASession;

    public delegate void EndASASessionDelegate();
    public event EndASASessionDelegate OnEndASASession;

    public delegate void CreateAnchorDelegate();
    public event CreateAnchorDelegate OnCreateAnchorStarted;
    public event CreateAnchorDelegate OnCreateAnchorSucceeded;
    public event CreateAnchorDelegate OnCreateAnchorFailed;

    public delegate void CreateLocalAnchorDelegate();
    public event CreateLocalAnchorDelegate OnCreateLocalAnchor;

    public delegate void RemoveLocalAnchorDelegate();
    public event RemoveLocalAnchorDelegate OnRemoveLocalAnchor;

    public delegate void FindAnchorDelegate();
    public event FindAnchorDelegate OnFindASAAnchor;

    public delegate void AnchorLocatedDelegate();
    public event AnchorLocatedDelegate OnASAAnchorLocated;

    public delegate void DeleteASAAnchorDelegate();
    public event DeleteASAAnchorDelegate OnDeleteASAAnchor;
    #endregion
}
