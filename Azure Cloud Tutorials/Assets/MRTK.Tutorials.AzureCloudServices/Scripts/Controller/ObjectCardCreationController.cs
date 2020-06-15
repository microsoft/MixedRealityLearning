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
    public class ObjectCardCreationController : MonoBehaviour
    {
        [Header("Managers")]
        [SerializeField]
        private MainSceneManager sceneManager;
        [Header("UI Elements")]
        [SerializeField]
        private ComputerVisionController computerVisionController;
        [SerializeField]
        private TMP_Text objectNameLabel;
        [SerializeField]
        private TMP_Text messageLabel;
        [SerializeField]
        private TMP_InputField descriptionInputField;
        [SerializeField]
        private Image thumbnailImage;
        [SerializeField]
        private Sprite thumbnailPlaceHolderImage;
        [SerializeField]
        private Interactable[] buttons;
        
        private TrackedObject trackedObject;
        
        private void Awake()
        {
            if (sceneManager == null)
            {
                sceneManager = FindObjectOfType<MainSceneManager>();
            }
        }
        
        public async void Init(TrackedObject source)
        {
            trackedObject = source;
            objectNameLabel.SetText(this.trackedObject.Name);
            descriptionInputField.text = this.trackedObject.Description;
            SetButtonsInteractiveState(true);
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

        public async void TakeThumbnailPhoto()
        {
            SetButtonsInteractiveState(false);
            
            var photo = await sceneManager.TakePhotoWithThumbnail();
            thumbnailImage.sprite = photo.Texture.CreateSprite();
            
            messageLabel.text = "Uploading Thumbnail, please wait ...";
            var blobName = await sceneManager.DataManager.UploadBlob(photo.ImageData, trackedObject.Name + "_thumbnail.png");
            trackedObject.ThumbnailBlobName = blobName;
            SaveChanges();
            
            SetButtonsInteractiveState(true);
        }

        public async void DeleteThumbnailPhoto()
        {
            if (!string.IsNullOrEmpty(trackedObject.ThumbnailBlobName))
            {
                if (!await sceneManager.DataManager.DeleteBlob(trackedObject.ThumbnailBlobName))
                {
                    return;
                }
                
                thumbnailImage.sprite = thumbnailPlaceHolderImage;
                trackedObject.ThumbnailBlobName = "";
                SaveChanges();
            }
            else
            {
                thumbnailImage.sprite = thumbnailPlaceHolderImage;
            }
        }

        public async void SaveChanges()
        {
            SetButtonsInteractiveState(false);
            UpdateProjectData();
            messageLabel.text = "Updating data, please wait ...";
            var success = await sceneManager.DataManager.UploadOrUpdate(trackedObject);
            messageLabel.text = success ? "Updated data in database." : "Failed to update database.";
            SetButtonsInteractiveState(true);
        }

        public void OnComputerVisionButtonClick()
        {
            if (!string.IsNullOrWhiteSpace(trackedObject.CustomVisionTagId))
            {
                messageLabel.text = "This object has been already trained for custom vision object.";
                return;
            }
            
            computerVisionController.gameObject.SetActive(true);
            computerVisionController.Init(trackedObject);
            gameObject.SetActive(false);
        }
        
        public void OnSaveLocationButtonClick()
        {
            if (!string.IsNullOrWhiteSpace(trackedObject.SpatialAnchorId))
            {
                messageLabel.text = "There is already an anchor location saved for this object.";
                return;
            }

            SetButtonsInteractiveState(false);
            messageLabel.text = "Move pointer and AirTap on the desired place to save the location.";
            sceneManager.AnchorManager.StartPlacingAnchor(trackedObject);
            sceneManager.AnchorManager.OnCreateAnchorSucceeded += HandleOnCreateAnchorSucceeded;
        }

        private async void HandleOnCreateAnchorSucceeded(object sender, string id)
        {
            sceneManager.AnchorManager.OnCreateAnchorSucceeded -= HandleOnCreateAnchorSucceeded;
            trackedObject.SpatialAnchorId = id;
            await sceneManager.DataManager.UploadOrUpdate(trackedObject);
            sceneManager.OpenMainMenu();
            gameObject.SetActive(false);
        }

        private void UpdateProjectData()
        {
            trackedObject.Description = descriptionInputField.text;
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
