using System.Linq;
using System.Threading.Tasks;
using MRTK.Tutorials.AzureCloudPower.Domain;
using MRTK.Tutorials.AzureCloudPower.Managers;
using UnityEngine;
using UnityEngine.Events;

namespace MRTK.Tutorials.AzureCloudPower.Test
{
    public class AzureVisionTestSceneManager : MonoBehaviour
    {
        [SerializeField]
        private string testProjectName = "testProject";
        [SerializeField]
        private string testTagName = "myTestTag";

        [SerializeField]
        private string publishedModelName = "ModelIteration1";
        [SerializeField]
        private ObjectDetectionManager objectDetectionManager;
        [SerializeField]
        private DataManager dataManager;
        [SerializeField]
        private UnityEvent onTakingPhotoStarted;
        [SerializeField]
        private UnityEvent onTakingPhotoDone;

        public async void CreateNewProject()
        {
            Debug.Log($"Checking of project {testProjectName} already exists.");
            var trackedObject = await dataManager.FindByName(testProjectName);
            if (trackedObject != null)
            {
                Debug.Log($"Project {testProjectName} already exists, skipping creation.");

                return;
            }

            trackedObject = new ObjectProject()
            {
                Name = testProjectName,
                CustomVision = new CustomVision()
                {
                    ProjectId = objectDetectionManager.ProjectId
                }
            };

            Debug.Log($"Uploading Project {testProjectName}...");
            var success = await dataManager.UploadOrUpdate(trackedObject);
            Debug.Log($"Uploading Project {testProjectName} result was successful: {success}");
        }

        public async void CreateNewTag()
        {
            var trackedObject = await dataManager.FindByName(testProjectName);
            if (trackedObject == null)
            {
                Debug.Log($"Project {testProjectName} does not exists, skipping creation.");
                return;
            }

            if (!string.IsNullOrWhiteSpace(trackedObject.CustomVision.TagId))
            {
                Debug.Log($"Project {testProjectName} already has a tag id.");
                return;
            }

            var tagCreationResult = await objectDetectionManager.CreateTag(testTagName);
            trackedObject.CustomVision.TagName = testTagName;
            trackedObject.CustomVision.TagId = tagCreationResult.Id;
            Debug.Log($"Uploading Project {testProjectName}...");
            var success = await dataManager.UploadOrUpdate(trackedObject);
            Debug.Log($"Uploading Project {testProjectName} result was successful: {success}");
        }

        public async void UploadImagesForTrackedObject()
        {
            var trackedObject = await dataManager.FindByName(testProjectName);
            if (trackedObject == null)
            {
                Debug.Log($"Project {testProjectName} does not exists, skipping creation.");
                return;
            }
            if (string.IsNullOrWhiteSpace(trackedObject.CustomVision.TagId))
            {
                Debug.Log($"Project {testProjectName} has no tag id.");
                return;
            }
            if (!objectDetectionManager.IsCameraActive)
            {
                Debug.Log("Camera is not ready.");
                return;
            }

            var imagesToTake = 5;
            while (imagesToTake > 0)
            {
                Debug.Log($"Images to take {imagesToTake}...");

                onTakingPhotoStarted?.Invoke();
                var img = await objectDetectionManager.TakePhoto();
                onTakingPhotoDone?.Invoke();

                Debug.Log("Uploading image...");
                var result = await objectDetectionManager.UploadTrainingImage(img, trackedObject.CustomVision.TagId);
                Debug.Log("Upload done!");
                trackedObject.CustomVision.ImageIds.Add(result.Images[0].Image.Id);
                Debug.Log($"New image uploaded with Id {result.Images[0].Image.Id}.");

                await Task.Delay(500);

                imagesToTake--;
            }

            Debug.Log($"Uploading Project {testProjectName}...");
            var success = await dataManager.UploadOrUpdate(trackedObject);
            Debug.Log($"Uploading Project {testProjectName} result was successful: {success}");
        }

        public async void AnalyzeFromWebCam()
        {
            Debug.Log("Testing image detection from web cam.");

            if (!objectDetectionManager.IsCameraActive)
            {
                Debug.Log("Camera is not ready.");
                return;
            }

            onTakingPhotoStarted?.Invoke();
            var img = await objectDetectionManager.TakePhoto();
            onTakingPhotoDone?.Invoke();
            var result = await objectDetectionManager.DetectImage(img, publishedModelName);
            var mostProbable = result.Predictions.OrderByDescending(p => p.Probability).First();
            Debug.Log($"Image recognition result: Found '{mostProbable.TagName}' with probability [{mostProbable.Probability}].");
        }

        public async void UploadFromWebCam()
        {
            Debug.Log("Testing image upload from web cam.");

            if (!objectDetectionManager.IsCameraActive)
            {
                Debug.Log("Camera is not ready.");
                return;
            }
            var trackedObject = await dataManager.FindByName(testProjectName);
            if (trackedObject == null)
            {
                Debug.Log($"Project {testProjectName} does not exists, skipping creation.");
                return;
            }
            if (string.IsNullOrWhiteSpace(trackedObject.CustomVision.TagId))
            {
                Debug.Log($"Project {testProjectName} has no tag id.");
                return;
            }

            onTakingPhotoStarted?.Invoke();
            var img = await objectDetectionManager.TakePhoto();
            onTakingPhotoDone?.Invoke();
            var result = await objectDetectionManager.UploadTrainingImage(img, trackedObject.CustomVision.TagId);
            Debug.Log($"Image uploaded with result: '{result.IsBatchSuccessful}'.");
        }

        public async void CheckIterationStatus()
        {
            var trackedObject = await dataManager.FindByName(testProjectName);
            if (trackedObject == null)
            {
                Debug.Log($"Project {testProjectName} does not exists, skipping creation.");
                return;
            }
            var status = await objectDetectionManager.GetIterationStatus(trackedObject.CustomVision.IterationId);
            Debug.Log(status.Status);
        }
    }
}