// Copyright (c) Microsoft Corporation. 
// Licensed under the MIT License.

using System.Threading.Tasks;
using MixedReality.Toolkit;
using MRTK.Tutorials.AzureCloudServices.Scripts.Domain;
using MRTK.Tutorials.AzureCloudServices.Scripts.Managers;
using MRTK.Tutorials.AzureCloudServices.Scripts.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MRTK.Tutorials.AzureCloudServices.Scripts.Controller
{
    /// <summary>
    /// Handles UI/UX for creation of a new tracked object.
    /// </summary>
    public class ObjectEditController : MonoBehaviour
    {
        [Header("Managers")]
        [SerializeField]
        private SceneController sceneController;
        [Header("UI Elements")]
        [SerializeField]
        private ComputerVisionController computerVisionController = default;
        [SerializeField]
        private GameObject hintTextPrefab = default;
        [SerializeField]
        private TMP_Text objectNameLabel = default;
        [SerializeField]
        private TMP_Text messageLabel = default;
        [SerializeField]
        private Text descriptionInputField = default;
        [SerializeField]
        private Image thumbnailImage = default;
        [SerializeField]
        private Sprite thumbnailPlaceHolderImage = default;
        [SerializeField]
        private StatefulInteractable[] buttons = default;
        [SerializeField] private InputActionReference leftHandTapActionReference = null;
        [SerializeField] private InputActionReference rightHandTapActionReference = null;

        private TrackedObject trackedObject;
        private GameObject hintTextInstance;
        private bool isWaitingForAirtap = false;

        private void Awake()
        {
            if (sceneController == null)
            {
                sceneController = FindObjectOfType<SceneController>();
            }
        }

        private void Start()
        {
            if (hintTextInstance == null)
            {
                hintTextInstance = Instantiate(hintTextPrefab, Camera.main.transform);
                hintTextInstance.SetActive(false);
            }
            InputAction placementActionRH = GetInputActionFromReference(rightHandTapActionReference);
            if (placementActionRH == null)
            {
                Debug.Log("Failed to register the tap action for right hand, the action reference was null or contained no action.");
            }
            else
            {
                placementActionRH.performed += StartTap;
            }
            
            InputAction placementActionLH = GetInputActionFromReference(leftHandTapActionReference);
            if (placementActionLH == null)
            {
                Debug.Log("Failed to register the tap action for left hand, the action reference was null or contained no action.");
            }
            else
            {
                placementActionLH.performed += StartTap;
            }
        }

        private void StartTap(InputAction.CallbackContext obj)
        {
            if (isWaitingForAirtap)
            {
                CapturePhoto();
            }
        }

        /// <summary>
        /// Extracts the InputAction from the InputActionReference.
        /// </summary>
        /// <param name="actionReference">
        /// The InputActionReference containing the desired InputAction.
        /// </param>
        /// <returns>An InputAction, or null.</returns>
        public static InputAction GetInputActionFromReference(InputActionReference actionReference)
        {
            if (actionReference == null)
            {
                return null;
            }

            return actionReference.action;
        }

        /// <summary>
        /// Init the menu with the given TrackedObject.
        /// Should be called from the previous menu.
        /// </summary>
        /// <param name="source">TrackedObject source</param>
        public async void Init(TrackedObject source)
        {
            trackedObject = source;
            objectNameLabel.SetText(trackedObject.Name);
            descriptionInputField.text = trackedObject.Description;
            SetButtonsInteractiveState(true);
            if (!string.IsNullOrEmpty(trackedObject.ThumbnailBlobName))
            {
                thumbnailImage.sprite = await LoadThumbnailImage();
            }
            else
            {
                thumbnailImage.sprite = thumbnailPlaceHolderImage;
            }
            
            sceneController.StartCamera();
        }

        /// <summary>
        /// Take a thumbnail photo for the TrackedObject and upload to azure blob storage.
        /// </summary>
        public void TakeThumbnailPhoto()
        {
            if (!sceneController.IsCameraActive)
            {
                messageLabel.text = "Camera is not ready or accessible.";
                return;
            }
            
            SetButtonsInteractiveState(false);
            hintTextInstance.SetActive(true);
            isWaitingForAirtap = true;
            messageLabel.text = "Look at object and do Airtap to take photo.";

        }

        /// <summary>
        /// Delete a thumbnail photo from azure blob storage.
        /// </summary>
        public async void DeleteThumbnailPhoto()
        {
            if (!string.IsNullOrEmpty(trackedObject.ThumbnailBlobName))
            {
                if (!await sceneController.DataManager.DeleteBlob(trackedObject.ThumbnailBlobName))
                {
                    return;
                }
                
                thumbnailImage.sprite = thumbnailPlaceHolderImage;
                trackedObject.ThumbnailBlobName = string.Empty;
                SaveChanges();
            }
            else
            {
                thumbnailImage.sprite = thumbnailPlaceHolderImage;
            }
        }

        /// <summary>
        /// Save changes for the TrackedObject into the azure table storage.
        /// </summary>
        public async void SaveChanges()
        {
            SetButtonsInteractiveState(false);
            trackedObject.Description = descriptionInputField.text;
            messageLabel.text = "Updating data, please wait ...";
            var success = await sceneController.DataManager.UploadOrUpdate(trackedObject);
            messageLabel.text = success ? "Updated data in database." : "Failed to update database.";
            SetButtonsInteractiveState(true);
        }

        /// <summary>
        /// Start UX flow for taking images for azure custom vision.
        /// </summary>
        public void OpenComputerVisionFlow()
        {
            if (trackedObject.HasBeenTrained)
            {
                messageLabel.text = "This object has been already trained for custom vision object.";
                return;
            }
            
            if (!sceneController.IsCameraActive)
            {
                messageLabel.text = "Camera is not ready or accessible.";
                return;
            }
            
            computerVisionController.gameObject.SetActive(true);
            computerVisionController.Init(trackedObject);
            gameObject.SetActive(false);
        }
        
        /// <summary>
        /// Start UX flow for storing the location via azure Spatial Anchors.
        /// </summary>
        public void OpenSpatialAnchorsFlow()
        {
            if (!string.IsNullOrWhiteSpace(trackedObject.SpatialAnchorId))
            {
                messageLabel.text = "There is already an anchor location saved for this object.";
                return;
            }

            sceneController.StopCamera();
            SetButtonsInteractiveState(false);
            messageLabel.text = "Move pointer and AirTap on the desired place to save the location.";
            sceneController.AnchorManager.StartPlacingAnchor(trackedObject);
            sceneController.AnchorManager.OnCreateAnchorSucceeded += HandleOnCreateAnchorSucceeded;
        }
        
        public void HandleOnPointerClick()
        {
            if (isWaitingForAirtap)
            {
                CapturePhoto();
            }
        }
        
        private async void CapturePhoto()
        {
            isWaitingForAirtap = false;
            hintTextInstance.SetActive(false);

            var photo = await sceneController.TakePhotoWithThumbnail();
            thumbnailImage.sprite = photo.Texture.CreateSprite();
            
            messageLabel.text = "Uploading Thumbnail, please wait ...";
            var blobName = await sceneController.DataManager.UploadBlob(photo.ImageData, trackedObject.Name + "_thumbnail.png");
            trackedObject.ThumbnailBlobName = blobName;
            SaveChanges();
            
            SetButtonsInteractiveState(true);
        }

        private async void HandleOnCreateAnchorSucceeded(object sender, string id)
        {
            sceneController.AnchorManager.OnCreateAnchorSucceeded -= HandleOnCreateAnchorSucceeded;
            sceneController.StartCamera();
            trackedObject.SpatialAnchorId = id;
            await sceneController.DataManager.UploadOrUpdate(trackedObject);
            sceneController.OpenMainMenu();
            gameObject.SetActive(false);
        }
        
        private async Task<Sprite> LoadThumbnailImage()
        {
            var imageData = await sceneController.DataManager.DownloadBlob(trackedObject.ThumbnailBlobName);
            var texture = new Texture2D(2, 2);
            texture.LoadImage(imageData);
            
            return texture.CreateSprite();
        }

        private void SetButtonsInteractiveState(bool state)
        {
            foreach (var interactable in buttons)
            {
                interactable.enabled = state;
            }
        }
    }
}
