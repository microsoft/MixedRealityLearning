using System.Threading.Tasks;
using Microsoft.MixedReality.Toolkit.UI;
using MRTK.Tutorials.AzureCloudPower.Domain;
using MRTK.Tutorials.AzureCloudPower.Managers;
using MRTK.Tutorials.AzureCloudServices.Scripts.Managers;
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
        
        private TrackedObjectProject project;
        
        private void Awake()
        {
            if (sceneManager == null)
            {
                sceneManager = FindObjectOfType<MainSceneManager>();
            }
        }

        public async void Init(TrackedObjectProject trackedObjectProject)
        {
            project = trackedObjectProject;
            objectNameLabel.SetText(project.Name);
            descriptionInputField.text = project.Description;
            if (!string.IsNullOrEmpty(project.ThumbnailBlobName))
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
            var imageData = await sceneManager.DataManager.DownloadBlob(project.ThumbnailBlobName);
            var texture = new Texture2D(2, 2);
            texture.LoadImage(imageData);
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        }

        public async void TakeThumbnailPhoto()
        {
            SetButtonsInteractiveState(false);
            byte[] imageData;
            if (Application.isEditor)
            {
                var texture = ScreenCapture.CaptureScreenshotAsTexture();
                thumbnailImage.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                imageData = texture.EncodeToPNG();
            }
            else
            {
                imageData = await sceneManager.ObjectDetectionManager.TakePhoto();
                var texture = new Texture2D(2, 2);
                texture.LoadImage(imageData);
                thumbnailImage.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            }
            
            messageLabel.text = "Uploading Thumbnail, please wait ...";
            var blobName = await sceneManager.DataManager.UploadBlob(imageData, project.Name + "_thumbnail.png");
            project.ThumbnailBlobName = blobName;
            SaveChanges();
            
            SetButtonsInteractiveState(true);
        }

        public async void DeleteThumbnailPhoto()
        {
            if (!string.IsNullOrEmpty(project.ThumbnailBlobName))
            {
                if (!await sceneManager.DataManager.DeleteBlob(project.ThumbnailBlobName))
                {
                    return;
                }
                
                thumbnailImage.sprite = thumbnailPlaceHolderImage;
                project.ThumbnailBlobName = "";
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
            var success = await sceneManager.DataManager.UploadOrUpdate(project);
            messageLabel.text = success ? "Updated data in database." : "Failed to update database.";
            SetButtonsInteractiveState(true);
        }

        public async void OnComputerVisionButtonClick()
        {
            if(!string.IsNullOrEmpty(project.CustomVision.IterationId))
            {
                var status = await sceneManager.ObjectDetectionManager.GetIterationStatus(project.CustomVision.IterationId);
                if (status.IsCompleted())
                {
                    messageLabel.text = "The model is already trained.";
                    return;
                }
            }
            
            computerVisionController.gameObject.SetActive(true);
            computerVisionController.Init(project);
            gameObject.SetActive(false);
        }
        
        public void OnSaveLocationButtonClick()
        {
            
        }

        private void UpdateProjectData()
        {
            project.Description = descriptionInputField.text;
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
