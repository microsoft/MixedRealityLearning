using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using MRTK.Tutorials.AzureCloudPower.Dtos;
using MRTK.Tutorials.AzureCloudPower.Utilities;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Windows.WebCam;

namespace MRTK.Tutorials.AzureCloudPower.Managers
{
    public class ObjectDetectionManager : MonoBehaviour
    {
        public bool IsCameraActive { private set; get; }
        public string ProjectId => projectId;

        [Header("Azure Settings")]
        [SerializeField]
        private string azureResourceSubcriptionId;
        [SerializeField]
        private string azureResourceGroupName;
        [SerializeField]
        private string cognitiveServiceResourceName;
        [SerializeField]
        private string resourceBaseEndpoint = "https://westeurope.api.cognitive.microsoft.com";

        [Header("Project Settings")]
        [SerializeField]
        private string apiKey;
        [SerializeField]
        private string projectId;

        [Header("Misc Settings")]
        [SerializeField]
        private bool autostartCamera = true;

        [Header("Events")]
        [SerializeField]
        private UnityEvent onCameraStarted;
        [SerializeField]
        private UnityEvent onCameraStopped;

        private PhotoCapture photoCapture;

        private void Start()
        {
            if (autostartCamera)
            {
                StartCamera();
            }
        }

        public void StartCamera()
        {
            if (IsCameraActive)
            {
                return;
            }

            Debug.Log("Starting camera system.");
            if (photoCapture == null)
            {
                PhotoCapture.CreateAsync(false, captureObject =>
                {
                    photoCapture = captureObject;
                    StartPhotoMode();
                });
            }
            else
            {
                StartPhotoMode();
            }
        }

        public void StopCamera()
        {
            if (!IsCameraActive || photoCapture == null)
            {
                return;
            }

            photoCapture.StopPhotoModeAsync(result =>
            {
                if (result.success)
                {
                    IsCameraActive = false;
                    onCameraStopped?.Invoke();
                }
            });
        }

        private void StartPhotoMode()
        {
            var resolution = PhotoCapture
                .SupportedResolutions
                .OrderByDescending((r) => r.width * r.height)
                .First();

            var cameraParams = new CameraParameters()
            {
                hologramOpacity = 0f,
                cameraResolutionWidth = resolution.width,
                cameraResolutionHeight = resolution.height,
                pixelFormat = CapturePixelFormat.JPEG
            };

            photoCapture.StartPhotoModeAsync(cameraParams, startResult =>
            {
                Debug.Log($"Camera system start result = {startResult.resultType}.");
                IsCameraActive = startResult.success;
                onCameraStarted?.Invoke();
            });
        }

        /// <summary>
        /// Take a photo from the WebCam. Make sure the camera is active.
        /// </summary>
        /// <returns>Image data encoded as jpg.</returns>
        public Task<byte[]> TakePhoto()
        {
            if (!IsCameraActive || photoCapture == null)
            {
                throw new Exception("Can't take photo when camera is not ready.");
            }

            return Task.Run(() =>
            {
                var completionSource = new TaskCompletionSource<byte[]>();

                AppDispatcher.Instance().Enqueue(() =>
                {
                    Debug.Log("Starting photo capture.");

                    photoCapture.TakePhotoAsync((photoCaptureResult, frame) =>
                    {
                        Debug.Log("Photo capture done.");

                        var buffer = new List<byte>();
                        frame.CopyRawImageDataIntoBuffer(buffer);
                        completionSource.TrySetResult(buffer.ToArray());
                    });
                });
                
                return completionSource.Task;
            });
        }

        /// <summary>
        /// Create a tag for the project to associate images with for later detection once a project is trained.
        /// </summary>
        /// <param name="tag">Name of the tag</param>
        /// <returns>Tag info with id.</returns>
        public async Task<TagCreationResult> CreateTag(string tag)
        {
            // https://docs.microsoft.com/en-us/rest/api/cognitiveservices/customvisiontraining/createtag/createtag
            
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Training-Key", apiKey);
                var result = await client.PostAsync(
                    $"{resourceBaseEndpoint}/customvision/v3.0/training/projects/{projectId}/tags?name={tag}", null);

                if (!result.IsSuccessStatusCode)
                {
                    throw new Exception(result.ReasonPhrase);
                }

                // store tag id in VisionProject.TagId
                var body = await result.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<TagCreationResult>(body);
            }
        }

        /// <summary>
        /// Upload an image to the project with a give tag id.
        /// </summary>
        /// <param name="image">Data of the image.</param>
        /// <param name="tagIds">Tag id to associate with the image.</param>
        /// <returns>Image data.</returns>
        public async Task<ImagesCreatedResult> UploadTrainingImage(byte[] image, string tagIds)
        {
            // https://docs.microsoft.com/en-us/rest/api/cognitiveservices/customvisiontraining/createimagesfromdata/createimagesfromdata

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Training-Key", apiKey);
                var content = new ByteArrayContent(image);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                var result = await client.PostAsync(
                    $"{resourceBaseEndpoint}/customvision/v3.0/training/projects/{projectId}/images?tagIds={tagIds}",
                    content);

                if (!result.IsSuccessStatusCode)
                {
                    throw new Exception(result.ReasonPhrase);
                }

                // Store image id in VisionProject.ImageIds
                var body = await result.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<ImagesCreatedResult>(body);
            }
        }

        /// <summary>
        /// Start training the project which returns a training iteration.
        /// Use the training iteration info to check later of the completion status.
        /// Hint: To start training you need to have at least 2 tags and 5 images per tag in the project.
        /// </summary>
        /// <returns>Status of the training iteration.</returns>
        public async Task<TrainProjectResult> TrainProject()
        {
            // https://docs.microsoft.com/en-us/rest/api/cognitiveservices/customvisiontraining/trainproject/trainproject

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Training-Key", apiKey);
                var result = await client.PostAsync(
                    $"{resourceBaseEndpoint}/customvision/v3.0/training/projects/{projectId}/train", null);

                if (!result.IsSuccessStatusCode)
                {
                    throw new Exception(result.ReasonPhrase);
                }

                // Id and name represent the iteration

                var body = await result.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<TrainProjectResult>(body);
            }
        }

        /// <summary>
        /// Get the status for a project training iteration.
        /// Hint: Use to check if the training is completed.
        /// </summary>
        /// <param name="iterationId">Target iteration to check.</param>
        /// <returns>Status of the training iteration.</returns>
        public async Task<TrainProjectResult> GetIterationStatus(string iterationId)
        {
            // https://docs.microsoft.com/en-us/rest/api/cognitiveservices/customvisiontraining/getiteration/getiteration

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Training-Key", apiKey);
                var result = await client.GetAsync($"{resourceBaseEndpoint}/customvision/v3.0/training/projects/{projectId}/iterations/{iterationId}");

                if (!result.IsSuccessStatusCode)
                {
                    throw new Exception(result.ReasonPhrase);
                }

                // Id and name represent the iteration

                var body = await result.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<TrainProjectResult>(body);
            }
        }

        /// <summary>
        /// Publish a trained iteration to be used for image detection.
        /// </summary>
        /// <param name="iterationId">Id of the trained iteration.</param>
        /// <param name="publishName">Name for the trained model which is used when detecting images.</param>
        /// <returns>Success status.</returns>
        public async Task<bool> PublishIteration(string iterationId, string publishName)
        {
            // https://docs.microsoft.com/en-us/rest/api/cognitiveservices/customvisiontraining/publishiteration/publishiteration

            using (var client = new HttpClient())
            {
                var predictionId = $"/subscriptions/{azureResourceSubcriptionId}/resourceGroups/{azureResourceGroupName}/providers/Microsoft.CognitiveServices/accounts/{cognitiveServiceResourceName}";
                client.DefaultRequestHeaders.Add("Training-Key", apiKey);
                var result = await client.PostAsync(
                    $"{resourceBaseEndpoint}/customvision/v3.0/training/projects/{projectId}/iterations/{iterationId}/publish?publishName={publishName}&predictionId={predictionId}", null);

                // store the publishName in VisionProject.PublishModelName

                return result.IsSuccessStatusCode;
            }
        }

        public async Task<ImagePredictionResult> DetectImage(byte[] image, string publishedName)
        {
            // https://docs.microsoft.com/en-us/rest/api/cognitiveservices/customvisionprediction/classifyimage/classifyimage

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Prediction-Key", apiKey);
                var result = await client.PostAsync(
                    $"{resourceBaseEndpoint}/customvision/v3.0/prediction/{projectId}/classify/iterations/{publishedName}/image",
                    new ByteArrayContent(image));

                if (!result.IsSuccessStatusCode)
                {
                    throw new Exception(result.ReasonPhrase);
                }

                var body = await result.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<ImagePredictionResult>(body);
            }
        }
    }
}
