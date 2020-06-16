using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.MixedReality.Toolkit.UI;
using MRTK.Tutorials.AzureCloudServices.Scripts.Domain;
using MRTK.Tutorials.AzureCloudServices.Scripts.Managers;
using MRTK.Tutorials.AzureCloudServices.Scripts.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MRTK.Tutorials.AzureCloudServices.Scripts.Controller
{
    /// <summary>
    /// Handles UI, UX and flow for computer vision menu.
    /// </summary>
    public class ComputerVisionController : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField]
        private string trainingModelPublishingName = "main_model";
        [Header("Manager")]
        [SerializeField]
        private SceneController sceneController;
        [Header("UI")]
        [SerializeField]
        private GameObject previousMenu;
        [SerializeField]
        private Image previewImage;
        [SerializeField]
        private TMP_Text messageLabel;
        [SerializeField]
        private Image[] images;
        [SerializeField]
        private Sprite thumbnailPlaceHolderImage;
        [SerializeField]
        private Interactable[] buttons;
        
        private TrackedObject trackedObject;
        private List<ImageThumbnail> imagesToCapture;
        private int currentImageIndex;
        private bool isWaitingForAirtap;
        
        private void Awake()
        {
            if (sceneController == null)
            {
                sceneController = FindObjectOfType<SceneController>();
            }
        }

        public void Init(TrackedObject source)
        {
            trackedObject = source;
            currentImageIndex = -1;
            imagesToCapture = new List<ImageThumbnail>();
            previewImage.sprite = thumbnailPlaceHolderImage;
            SetButtonsInteractiveState(true);
            foreach (var image in images)
            {
                image.sprite = thumbnailPlaceHolderImage;
            }
            sceneController.StartCamera();
        }

        public async void StartPhotoCapture()
        {
            if (isWaitingForAirtap)
            {
                return;
            }
            if (currentImageIndex == 6)
            {
                messageLabel.text = "You have enough images";
                return;
            }
            
            isWaitingForAirtap = true;
            SetButtonsInteractiveState(false);
            messageLabel.text = "Do AirTap to take a photo.";
            await Task.Delay(300);
        }

        public void DeleteCurrentPhoto()
        {
            if (currentImageIndex < 0)
            {
                return;
            }
            
            previewImage.sprite = thumbnailPlaceHolderImage;
            images[currentImageIndex].sprite = thumbnailPlaceHolderImage;
            if(imagesToCapture.Count > 0)
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
            
            // Check if there is already an existing iteration and delete it
            if(!string.IsNullOrEmpty(sceneController.CurrentProject.CustomVisionIterationId))
            {
                await sceneController.ObjectDetectionManager.DeleteTrainingIteration(sceneController.CurrentProject.CustomVisionIterationId);
                sceneController.CurrentProject.CustomVisionIterationId = "";
                await sceneController.DataManager.UpdateProject(sceneController.CurrentProject);
            }

            messageLabel.text = "Please wait, uploading images.";
            SetButtonsInteractiveState(false);
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

            var tries = 15;
            while (tries > 0)
            {
                await Task.Delay(1000);
                var status = await sceneController.ObjectDetectionManager.GetTrainingStatus(objectTrainingResult.Id);
                if (status.IsCompleted())
                {
                    var publishResult = await sceneController.ObjectDetectionManager.PublishTrainingIteration(objectTrainingResult.Id,
                        trainingModelPublishingName);
                    if (!publishResult)
                    {
                        Debug.LogError("Failed to publish, please check the custom vision portal for your project.");
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
                
                tries--;
            }
            
            SetButtonsInteractiveState(true);
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
            currentImageIndex++;

            messageLabel.text = "Taking photo, stand still.";
            var imageThumbnail = await sceneController.TakePhotoWithThumbnail();
            var sprite = imageThumbnail.Texture.CreateSprite();
            images[currentImageIndex].sprite = sprite;
            previewImage.sprite = sprite;
            messageLabel.text = "";
            
            imagesToCapture.Add(imageThumbnail);
            isWaitingForAirtap = false;
            SetButtonsInteractiveState(true);
        }
        
        private void SetButtonsInteractiveState(bool state)
        {
            foreach (var interactable in buttons)
            {
                interactable.IsEnabled = state;
            }
        }
    }
}
