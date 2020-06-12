using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.MixedReality.Toolkit.UI;
using MRTK.Tutorials.AzureCloudPower.Domain;
using MRTK.Tutorials.AzureCloudServices.Scripts.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MRTK.Tutorials.AzureCloudServices.Scripts.Controller
{
    public class ComputerVisionController : MonoBehaviour
    {
        [Header("Manager")]
        [SerializeField]
        private MainSceneManager sceneManager;
        [Header("UI")]
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
        
        private TrackedObjectProject project;
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

        public void Init(TrackedObjectProject trackedObjectProject)
        {
            project = trackedObjectProject;
            index = -1;
            imagesToCapture = new List<ImageThumbnail>();
            previewImage.sprite = thumbnailPlaceHolderImage;
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

            SetButtonsInteractiveState(false);
            messageLabel.text = "Do AirTap to take a photo.";
            await Task.Delay(300);
            isWaitingForAirtap = true;
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
            
            if(!string.IsNullOrEmpty(project.CustomVision.IterationId))
            {
                var status = await sceneManager.ObjectDetectionManager.GetIterationStatus(project.CustomVision.IterationId);
                if (status.IsCompleted())
                {
                    messageLabel.text = "The model is already trained.";
                    return;
                }
            }

            messageLabel.text = "Please wait, uploading images.";
            SetButtonsInteractiveState(false);
            var tagId = project.CustomVision.TagId;
            foreach (var imageThumbnail in imagesToCapture)
            {
                 await sceneManager.ObjectDetectionManager.UploadTrainingImage(imageThumbnail.PngData, tagId);
            }
            messageLabel.text = "All images have been uploaded!";
            var objectTrainingResult = await sceneManager.ObjectDetectionManager.TrainProject();
            messageLabel.text = "Started training process, please wait for completion.";
            project.CustomVision.IterationId = objectTrainingResult.Id;
            project.CustomVision.PublishModelName = objectTrainingResult.Name;

            await sceneManager.DataManager.UploadOrUpdate(project);

            var tries = 10;
            while (tries > 0)
            {
                await Task.Delay(1000);
                var status = sceneManager.ObjectDetectionManager.GetIterationStatus(objectTrainingResult.Id);
                if (status.IsCompleted)
                {
                    var publishResult = await sceneManager.ObjectDetectionManager.PublishIteration(objectTrainingResult.Id,
                        objectTrainingResult.Name);
                    if (!publishResult)
                    {
                        Debug.LogError("Failed to publish, please check the custom vision portal for your project.");
                    }
                    else
                    {
                        messageLabel.text = "Model training is done and ready for detection.";
                    }
                    break;
                }
                
                tries--;
            }
            
            SetButtonsInteractiveState(true);
        }

        private async void CapturePhoto()
        {
            messageLabel.text = "";
            index++;
            
            byte[] imageData;
            Texture2D texture;
            if (Application.isEditor)
            {
                texture = ScreenCapture.CaptureScreenshotAsTexture();
                imageData = texture.EncodeToPNG();
            }
            else
            {
                imageData = await sceneManager.ObjectDetectionManager.TakePhoto();
                texture = new Texture2D(2, 2);
                texture.LoadImage(imageData);
            }
            
            var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            images[index].sprite = sprite;
            previewImage.sprite = sprite;
            
            var imageThumbnail = new ImageThumbnail()
            {
                PngData = imageData,
                Texture = texture
            };
            imagesToCapture.Add(imageThumbnail);
            SetButtonsInteractiveState(true);
            isWaitingForAirtap = false;
        }
        
        private void SetButtonsInteractiveState(bool state)
        {
            foreach (var interactable in buttons)
            {
                interactable.IsEnabled = state;
            }
        }

        private class ImageThumbnail
        {
            public byte[] PngData { get; set; }
            public Texture2D Texture { get; set; }
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
