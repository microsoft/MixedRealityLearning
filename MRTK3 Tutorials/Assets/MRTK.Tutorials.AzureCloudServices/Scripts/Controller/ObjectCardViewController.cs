// Copyright (c) Microsoft Corporation. 
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Threading.Tasks;
using MixedReality.Toolkit;
using MRTK.Tutorials.AzureCloudServices.Scripts.Domain;
using MRTK.Tutorials.AzureCloudServices.Scripts.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MRTK.Tutorials.AzureCloudServices.Scripts.Controller
{
    public class ObjectCardViewController : MonoBehaviour
    {
        [Header("Managers")]
        [SerializeField]
        private SceneController sceneController;
        [Header("UI")]
        [SerializeField]
        private TMP_Text objectNameLabel = default;
        [SerializeField]
        private TMP_Text descriptionLabel = default;
        [SerializeField]
        private TMP_Text messageLabel = default;
        [SerializeField]
        private Image thumbnailImage = default;
        [SerializeField]
        private Sprite thumbnailPlaceHolderImage = default;
        [SerializeField]
        private StatefulInteractable[] buttons = default;
        
        private TrackedObject trackedObject;
        private bool isSearchingWithComputerVision;
        private bool objectDetectedWithComputerVision;
        
        private void Awake()
        {
            if (sceneController == null)
            {
                sceneController = FindObjectOfType<SceneController>();
            }
        }
        
        private void OnDisable()
        {
            sceneController.OpenMainMenu();
        }

        public async void Init(TrackedObject source)
        {
            if (sceneController == null)
            {
                sceneController = FindObjectOfType<SceneController>();
            }
            
            trackedObject = source;
            objectNameLabel.SetText(this.trackedObject.Name);
            descriptionLabel.text = this.trackedObject.Description;
            isSearchingWithComputerVision = false;
            objectDetectedWithComputerVision = false;
            
            if (!string.IsNullOrEmpty(this.trackedObject.ThumbnailBlobName))
            {
                thumbnailImage.sprite = await LoadThumbnailImage();
            }
            else
            {
                thumbnailImage.sprite = thumbnailPlaceHolderImage;
            }
        }

        public async void StartComputerVisionDetection()
        {
            sceneController.StartCamera();
            if (string.IsNullOrEmpty(trackedObject.CustomVisionTagId) 
                || string.IsNullOrEmpty(sceneController.CurrentProject.CustomVisionIterationId))
            {
                messageLabel.text = "There is no model trained set for this object.";
                return;
            }
            if (isSearchingWithComputerVision || objectDetectedWithComputerVision)
            {
                return;
            }
            
            SetButtonsInteractiveState(false);
            isSearchingWithComputerVision = true;
            messageLabel.text = "Look around for object...";
            await SearchWithComputerVision();
            sceneController.StopCamera();
        }

        public void StartFindLocation()
        {
            if (string.IsNullOrEmpty(trackedObject.SpatialAnchorId))
            {
                messageLabel.text = "No spatial anchor has been specified for this object.";
                return;
            }
            if (sceneController.AnchorManager.CheckIsAnchorActiveForTrackedObject(trackedObject.SpatialAnchorId))
            {
                messageLabel.text = "The spatial anchor for this object is already spawned.";
                sceneController.AnchorManager.GuideToAnchor(trackedObject.SpatialAnchorId);
                return;
            }
            
            sceneController.StopCamera();
            sceneController.AnchorManager.OnFindAnchorSucceeded += HandleOnAnchorFound;
            sceneController.AnchorManager.FindAnchor(trackedObject);
        }

        private void HandleOnAnchorFound(object sender, EventArgs e)
        {
            Debug.Log("ObjectCardViewController.HandleOnAnchorFound");
            sceneController.AnchorManager.OnFindAnchorSucceeded -= HandleOnAnchorFound;
            SetButtonsInteractiveState(true);
        }

        public void CloseCard()
        {
            isSearchingWithComputerVision = false;
            messageLabel.text = string.Empty;
            sceneController.OpenMainMenu();
            Destroy(gameObject);
        }

        private async Task SearchWithComputerVision()
        {
            while (isSearchingWithComputerVision)
            {
                await Task.Delay(1000);
                var image = await sceneController.TakePhoto();
                try
                {
                    var response = await sceneController.ObjectDetectionManager.DetectImage(image, sceneController.CurrentProject.CustomVisionPublishedModelName);
                    var prediction = response.Predictions.SingleOrDefault(p => p.TagId == trackedObject.CustomVisionTagId);
                    if(prediction != null && prediction.Probability > 0.75d)
                    {
                        objectDetectedWithComputerVision = true;
                        isSearchingWithComputerVision = false;
                        messageLabel.text = "Object found!";
                    }
                }
                catch (Exception e)
                {
                    Debug.Log(e.Message);
                    isSearchingWithComputerVision = false;
                    objectDetectedWithComputerVision = false;
                    messageLabel.text = "Server error, try later again.";
                    
                    SetButtonsInteractiveState(true);
                }
            }
            
            SetButtonsInteractiveState(true);
        }

        private async Task<Sprite> LoadThumbnailImage()
        {
            var imageData = await sceneController.DataManager.DownloadBlob(trackedObject.ThumbnailBlobName);
            var texture = new Texture2D(2, 2);
            texture.LoadImage(imageData);
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
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
