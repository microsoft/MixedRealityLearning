using System.Threading.Tasks;
using Microsoft.MixedReality.Toolkit.UI;
using MRTK.Tutorials.AzureCloudPower.Managers;
using MRTK.Tutorials.AzureCloudServices.Scripts.Managers;
using TMPro;
using UnityEngine;

namespace MRTK.Tutorials.AzureCloudServices.Scripts.Controller
{
    public class ObjectCardCreationController : MonoBehaviour
    {
        [Header("Managers")]
        [SerializeField]
        private MainSceneManager sceneManager;
        [SerializeField]
        private DataManager dataManager;
        [SerializeField]
        private ObjectDetectionManager objectDetectionManager;
        [Header("UI Elements")]
        [SerializeField]
        private TMP_Text objectNameLabel;
        [SerializeField]
        private TMP_Text messageLabel;
        [SerializeField]
        private TMP_InputField descriptionInputField;
        [SerializeField]
        private SpriteRenderer thumbnailSpriteRenderer;
        [SerializeField]
        private Sprite thumbnailPlaceHolderImage;
        [SerializeField]
        private Interactable[] buttons;
        
        private void Awake()
        {
            if (sceneManager == null)
            {
                sceneManager = FindObjectOfType<MainSceneManager>();
            }
            if (dataManager == null)
            {
                dataManager = FindObjectOfType<DataManager>();
            }
            if (objectDetectionManager == null)
            {
                objectDetectionManager = FindObjectOfType<ObjectDetectionManager>();
            }
        }

        public async void Init()
        {
            objectNameLabel.SetText(sceneManager.CurrentProject.Name);
            descriptionInputField.text = sceneManager.CurrentProject.Description;
            if (!string.IsNullOrEmpty(sceneManager.CurrentProject.ThumbnailBlobName))
            {
                thumbnailSpriteRenderer.sprite = await LoadThumbnailImage();
            }
            else
            {
                thumbnailSpriteRenderer.sprite = thumbnailPlaceHolderImage;
            }
        }

        private async Task<Sprite> LoadThumbnailImage()
        {
            var imageData = await dataManager.DownloadBlob(sceneManager.CurrentProject.ThumbnailBlobName);
            var texture = new Texture2D(2, 2);
            texture.LoadImage(imageData);
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        }

        public async void TakeThumbnailPhoto()
        {
            SetButtonsInteractiveState(false);
            if (Application.isEditor)
            {
                var texture = ScreenCapture.CaptureScreenshotAsTexture();
                thumbnailSpriteRenderer.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                var pngData = texture.EncodeToPNG();
                messageLabel.text = "Uploading Thumbnail, please wait ...";
                var blobName = await dataManager.UploadBlob(pngData, sceneManager.CurrentProject.Name + "_thumbnail.png");
                sceneManager.CurrentProject.ThumbnailBlobName = blobName;
                SaveChanges();
            }
            else
            {
                var imageData = await objectDetectionManager.TakePhoto();
                var texture = new Texture2D(2, 2);
                texture.LoadImage(imageData);
                thumbnailSpriteRenderer.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                messageLabel.text = "Uploading Thumbnail, please wait ...";
                var blobName = await dataManager.UploadBlob(imageData, sceneManager.CurrentProject.Name + "_thumbnail.png");
                sceneManager.CurrentProject.ThumbnailBlobName = blobName;
                SaveChanges();
            }
            SetButtonsInteractiveState(true);
        }

        public async void DeleteThumbnailPhoto()
        {
            if (!string.IsNullOrEmpty(sceneManager.CurrentProject.ThumbnailBlobName))
            {
                if (!await dataManager.DeleteBlob(sceneManager.CurrentProject.ThumbnailBlobName))
                {
                    return;
                }
                
                thumbnailSpriteRenderer.sprite = thumbnailPlaceHolderImage;
                sceneManager.CurrentProject.ThumbnailBlobName = "";
                SaveChanges();
            }
            else
            {
                thumbnailSpriteRenderer.sprite = thumbnailPlaceHolderImage;
            }
        }

        public async void SaveChanges()
        {
            SetButtonsInteractiveState(false);
            UpdateProjectData();
            messageLabel.text = "Updating data, please wait ...";
            var success = await dataManager.UploadOrUpdate(sceneManager.CurrentProject);
            messageLabel.text = success ? "Updated data in database." : "Failed to update database.";
            SetButtonsInteractiveState(true);
        }

        private void UpdateProjectData()
        {
            sceneManager.CurrentProject.Description = descriptionInputField.text;
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
