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
    public class ComputerVisionController : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField]
        private string trainingModelPublishingName = "main_model";
        [Header("Manager")]
        [SerializeField]
        private MainSceneManager sceneManager;
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
        private int index;
        private List<ImageThumbnail> imagesToCapture;
        private bool isWaitingForAirtap;
        
        private void Awake()
        {
            if (sceneManager == null)
            {
                sceneManager = FindObjectOfType<MainSceneManager>();
            }
        }

        public void Init(TrackedObject source)
        {
            trackedObject = source;
            index = -1;
            imagesToCapture = new List<ImageThumbnail>();
            previewImage.sprite = thumbnailPlaceHolderImage;
            SetButtonsInteractiveState(true);
            foreach (var image in images)
            {
                image.sprite = thumbnailPlaceHolderImage;
            }
        }

        public async void OnCapturePhotoButtonClick()
        {
            if (isWaitingForAirtap)
            {
                return;
            }
            if (index == 6)
            {
                messageLabel.text = "You have enough images";
                return;
            }
            
            isWaitingForAirtap = true;
            SetButtonsInteractiveState(false);
            messageLabel.text = "Do AirTap to take a photo.";
            await Task.Delay(300);
        }

        public void OnDeleteButtonClick()
        {
            if (index < 0)
            {
                return;
            }
            
            previewImage.sprite = thumbnailPlaceHolderImage;
            images[index].sprite = thumbnailPlaceHolderImage;
            if(imagesToCapture.Count > 0)
            {
                imagesToCapture.Remove(imagesToCapture[index]);
            }
            
            index--;
        }

        public async void OnTrainButtonClick()
        {
            if (imagesToCapture.Count < 6)
            {
                messageLabel.text = "Not enough images to train the model";
                return;
            }
            
            // Check if there is already an existing iteration and delete it
            if(!string.IsNullOrEmpty(sceneManager.CurrentProject.CustomVisionIterationId))
            {
                await sceneManager.ObjectDetectionManager.DeleteTrainingIteration(sceneManager.CurrentProject.CustomVisionIterationId);
                sceneManager.CurrentProject.CustomVisionIterationId = "";
                await sceneManager.DataManager.UpdateProject(sceneManager.CurrentProject);
            }

            messageLabel.text = "Please wait, uploading images.";
            SetButtonsInteractiveState(false);
            var tagId = trackedObject.CustomVisionTagId;
            foreach (var imageThumbnail in imagesToCapture)
            {
                 await sceneManager.ObjectDetectionManager.UploadTrainingImage(imageThumbnail.ImageData, tagId);
            }
            messageLabel.text = "All images have been uploaded!";
            var objectTrainingResult = await sceneManager.ObjectDetectionManager.TrainProject();
            messageLabel.text = "Started training process, please wait for completion.";
            sceneManager.CurrentProject.CustomVisionIterationId = objectTrainingResult.Id;
            await sceneManager.DataManager.UpdateProject(sceneManager.CurrentProject);

            var tries = 15;
            while (tries > 0)
            {
                await Task.Delay(1000);
                var status = await sceneManager.ObjectDetectionManager.GetTrainingStatus(objectTrainingResult.Id);
                if (status.IsCompleted())
                {
                    var publishResult = await sceneManager.ObjectDetectionManager.PublishTrainingIteration(objectTrainingResult.Id,
                        trainingModelPublishingName);
                    if (!publishResult)
                    {
                        Debug.LogError("Failed to publish, please check the custom vision portal for your project.");
                    }
                    else
                    {
                        sceneManager.CurrentProject.CustomVisionPublishedModelName = trainingModelPublishingName;
                        await sceneManager.DataManager.UpdateProject(sceneManager.CurrentProject);

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

        private async void CapturePhoto()
        {
            index++;

            messageLabel.text = "Taking photo, stand still.";
            var imageThumbnail = await sceneManager.TakePhotoWithThumbnail();
            var sprite = imageThumbnail.Texture.CreateSprite();
            images[index].sprite = sprite;
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

        public void HandleOnTap()
        {
            if (isWaitingForAirtap)
            {
                CapturePhoto();
            }
        }
    }
}
