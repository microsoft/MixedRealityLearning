// Copyright (c) Microsoft Corporation. 
// Licensed under the MIT License.

using System.Collections.Generic;
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
    /// Handles UI, UX and flow for computer vision menu.
    /// </summary>
    public class ComputerVisionController : MonoBehaviour
    {
        [Header("Settings")] [SerializeField] private string trainingModelPublishingName = "main_model";
        [Header("Manager")] [SerializeField] private SceneController sceneController;
        [Header("UI")] [SerializeField] private GameObject hintTextPrefab = default;
        [SerializeField] private GameObject previousMenu = default;
        [SerializeField] private Image previewImage = default;
        [SerializeField] private TMP_Text messageLabel = default;
        [SerializeField] private Image[] images = default;
        [SerializeField] private Sprite thumbnailPlaceHolderImage = default;

        [SerializeField]
        private StatefulInteractable[] buttons = default;
        [SerializeField] private InputActionReference leftHandTapActionReference = null;
        [SerializeField] private InputActionReference rightHandTapActionReference = null;
        
        private TrackedObject trackedObject;
        private List<ImageThumbnail> imagesToCapture;
        private GameObject hintTextInstance;
        private int currentImageIndex;
        private bool isWaitingForAirtap = false;
        private bool isProcessingPhoto;

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
            if (isWaitingForAirtap && !isProcessingPhoto)
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

        public async void Init(TrackedObject source)
        {
            trackedObject = source;
            currentImageIndex = -1;
            imagesToCapture = new List<ImageThumbnail>();
            previewImage.sprite = thumbnailPlaceHolderImage;
            foreach (var image in images)
            {
                image.sprite = thumbnailPlaceHolderImage;
            }

            if (string.IsNullOrWhiteSpace(trackedObject.CustomVisionTagName))
            {
                messageLabel.text = "Setting up custom vision project.";

                var tagName = $"{trackedObject.Name}";
                var tagCreation = await sceneController.ObjectDetectionManager.CreateTag(tagName);
                trackedObject.CustomVisionTagName = tagCreation.Name;
                trackedObject.CustomVisionTagId = tagCreation.Id;
                await sceneController.DataManager.UploadOrUpdate(trackedObject);

                messageLabel.text = string.Empty;
            }

            SetButtonsInteractiveState(true);
            sceneController.StartCamera();
        }

        public void StartPhotoCapture()
        {
            Debug.Log("StartPhotoCapture");
            if (isWaitingForAirtap || isProcessingPhoto)
            {
                Debug.Log("isWaitingForAirtap || isProcessingPhoto");

                return;
            }

            if (currentImageIndex == 6)
            {
                messageLabel.text = "You have enough images";
                return;
            }

            hintTextInstance.SetActive(true);
            isWaitingForAirtap = true;
            SetButtonsInteractiveState(false);
            messageLabel.text = "Do AirTap to take a photo.";
        }

        public void DeleteCurrentPhoto()
        {
            if (currentImageIndex < 0)
            {
                return;
            }

            previewImage.sprite = thumbnailPlaceHolderImage;
            images[currentImageIndex].sprite = thumbnailPlaceHolderImage;
            if (imagesToCapture.Count > 0)
            {
                imagesToCapture.Remove(imagesToCapture[currentImageIndex]);
            }

            currentImageIndex--;
        }

        public async void StartModelTraining()
        {
            if (imagesToCapture.Count < 6)
            {
                messageLabel.text = "Not enough images to train the model";
                return;
            }

            SetButtonsInteractiveState(false);

            // Check if there is already an existing iteration and delete it
            if (!string.IsNullOrEmpty(sceneController.CurrentProject.CustomVisionIterationId))
            {
                await sceneController.ObjectDetectionManager.DeleteTrainingIteration(sceneController.CurrentProject.CustomVisionIterationId);
                sceneController.CurrentProject.CustomVisionIterationId = string.Empty;
                await sceneController.DataManager.UpdateProject(sceneController.CurrentProject);
            }

            messageLabel.text = "Please wait, uploading images.";
            var tagId = trackedObject.CustomVisionTagId;
            foreach (var imageThumbnail in imagesToCapture)
            {
                await sceneController.ObjectDetectionManager.UploadTrainingImage(imageThumbnail.ImageData, tagId);
            }

            messageLabel.text = "All images have been uploaded!";
            var objectTrainingResult = await sceneController.ObjectDetectionManager.TrainProject();
            messageLabel.text = "Started training process, please wait for completion.";
            sceneController.CurrentProject.CustomVisionIterationId = objectTrainingResult.Id;
            await sceneController.DataManager.UpdateProject(sceneController.CurrentProject);

            var tries = 0;
            while (tries < 180)
            {
                await Task.Delay(1000);
                var status = await sceneController.ObjectDetectionManager.GetTrainingStatus(objectTrainingResult.Id);

                if (status.IsCompleted())
                {
                    var publishResult = await sceneController.ObjectDetectionManager.PublishTrainingIteration(objectTrainingResult.Id,
                        trainingModelPublishingName);
                    if (!publishResult)
                    {
                        messageLabel.text = "Failed to publish, please check the custom vision portal of your project.";
                    }
                    else
                    {
                        trackedObject.HasBeenTrained = true;
                        await sceneController.DataManager.UploadOrUpdate(trackedObject);
                        sceneController.CurrentProject.CustomVisionPublishedModelName = trainingModelPublishingName;
                        await sceneController.DataManager.UpdateProject(sceneController.CurrentProject);

                        messageLabel.text = "Model training is done and ready for detection.";
                        await Task.Delay(1000);
                        previousMenu.SetActive(true);
                        gameObject.SetActive(false);
                    }

                    break;
                }

                tries++;
            }

            SetButtonsInteractiveState(true);
        }

        public void HandleOnPointerClick()
        {
            if (isWaitingForAirtap && !isProcessingPhoto)
            {
                CapturePhoto();
            }
        }

        private async void CapturePhoto()
        {
            isWaitingForAirtap = false;
            hintTextInstance.SetActive(false);
            if (isProcessingPhoto || currentImageIndex == 6)
            {
                SetButtonsInteractiveState(true);
                return;
            }
            isProcessingPhoto = true;
            currentImageIndex++;
            messageLabel.text = "Taking photo, stand still.";
            var imageThumbnail = await sceneController.TakePhotoWithThumbnail();
            
            var sprite = imageThumbnail.Texture.CreateSprite();
            images[currentImageIndex].sprite = sprite;
            previewImage.sprite = sprite;
            messageLabel.text = string.Empty;
            imagesToCapture.Add(imageThumbnail);
            Debug.Log("Taking photo done.");
            isProcessingPhoto = false;
            SetButtonsInteractiveState(true);
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