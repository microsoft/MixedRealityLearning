using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.MixedReality.Toolkit.UI;
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
        private MainSceneManager sceneManager;
        [Header("UI")]
        [SerializeField]
        private TMP_Text objectNameLabel;
        [SerializeField]
        private TMP_Text descriptionLabel;
        [SerializeField]
        private TMP_Text messageLabel;
        [SerializeField]
        private Image thumbnailImage;
        [SerializeField]
        private Sprite thumbnailPlaceHolderImage;
        [SerializeField]
        private Interactable[] buttons;
        
        private TrackedObject trackedObject;
        private bool isSearchingWithComputerVision;
        private bool objectDetectedWithComputerVision;
        
        private void Awake()
        {
            if (sceneManager == null)
            {
                sceneManager = FindObjectOfType<MainSceneManager>();
            }
        }
        
        private void OnDisable()
        {
            sceneManager.OpenMainMenu();
        }

        public async void Init(TrackedObject source)
        {
            if (sceneManager == null)
            {
                sceneManager = FindObjectOfType<MainSceneManager>();
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
        
        private async Task<Sprite> LoadThumbnailImage()
        {
            var imageData = await sceneManager.DataManager.DownloadBlob(trackedObject.ThumbnailBlobName);
            var texture = new Texture2D(2, 2);
            texture.LoadImage(imageData);
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        }
        
        public async void OnComputerVisionButtonClick()
        {
            if (string.IsNullOrEmpty(trackedObject.CustomVisionTagId) 
                || string.IsNullOrEmpty(sceneManager.CurrentProject.CustomVisionIterationId))
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
        }
        
        public void OnFindLocationButtonClick()
        {
            if (string.IsNullOrEmpty(trackedObject.SpatialAnchorId))
            {
                messageLabel.text = "No spatial anchor has been specified for this object.";
                return;
            }
            if (sceneManager.AnchorManager.CheckIsAnchorActiveForTrackedObject(trackedObject.SpatialAnchorId))
            {
                messageLabel.text = "The spatial anchor for this object is already spawned.";
                sceneManager.AnchorManager.GuideToAnchor(trackedObject.SpatialAnchorId);
                return;
            }
            
            sceneManager.AnchorManager.OnFindAnchorSucceeded += HandleOnAnchorFound;
            sceneManager.AnchorManager.FindAnchor(trackedObject);
        }

        private void HandleOnAnchorFound(object sender, EventArgs e)
        {
            sceneManager.AnchorManager.OnFindAnchorSucceeded -= HandleOnAnchorFound;
            SetButtonsInteractiveState(true);
        }

        public void OnCloseButtonClick()
        {
            isSearchingWithComputerVision = false;
            messageLabel.text = "";
            sceneManager.OpenMainMenu();
            Destroy(gameObject);
        }

        private async Task SearchWithComputerVision()
        {
            while (isSearchingWithComputerVision)
            {
                await Task.Delay(1000);
                var image = await sceneManager.TakePhoto();
                try
                {
                    var response = await sceneManager.ObjectDetectionManager.DetectImage(image, sceneManager.CurrentProject.CustomVisionPublishedModelName);
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
                    isSearchingWithComputerVision = false;
                    objectDetectedWithComputerVision = false;
                    messageLabel.text = "Server error, try later again.";
                    
                    SetButtonsInteractiveState(true);
                }
            }
            
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
