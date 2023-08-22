// Copyright (c) Microsoft Corporation. 
// Licensed under the MIT License.

using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using MRTK.Tutorials.AzureCloudServices.Scripts.Dtos;
using Newtonsoft.Json;
using UnityEngine;

namespace MRTK.Tutorials.AzureCloudServices.Scripts.Managers
{
    public class ObjectDetectionManager : MonoBehaviour
    {
        [Header("Azure Settings")]
        [SerializeField]
        private string azureResourceSubscriptionId = default;
        [SerializeField]
        private string azureResourceGroupName = default;
        [SerializeField]
        private string cognitiveServiceResourceName = default;

        [Header("Endpoints and Keys")]
        [SerializeField]
        private string resourceBaseEndpoint = default;
        [SerializeField]
        private string resourceBasePredictionEndpoint = default;
        [SerializeField]
        private string apiKey = default;
        [SerializeField]
        private string apiPredictionKey = default;

        [Header("Project Settings")]
        [SerializeField]
        private string projectId = default;

        /// <summary>
        /// Create a tag for the project to associate images with for later detection once a project is trained.
        /// </summary>
        /// <param name="nameOfTag">Name of the tag</param>
        /// <returns>Tag info with id.</returns>
        public async Task<TagCreationResult> CreateTag(string nameOfTag)
        {
            // https://docs.microsoft.com/en-us/rest/api/cognitiveservices/customvisiontraining/createtag/createtag
            
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Training-Key", apiKey);
                var result = await client.PostAsync(
                    $"{resourceBaseEndpoint}/customvision/v3.3/training/projects/{projectId}/tags?name={nameOfTag}", null);

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
                    $"{resourceBaseEndpoint}/customvision/v3.3/training/projects/{projectId}/images?tagIds={tagIds}",
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
                    $"https://testkleincustomvision.cognitiveservices.azure.com/customvision/v3.3/training/projects/{projectId}/train", null);

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
        public async Task<TrainProjectResult> GetTrainingStatus(string iterationId)
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
        public async Task<bool> PublishTrainingIteration(string iterationId, string publishName)
        {
            // https://docs.microsoft.com/en-us/rest/api/cognitiveservices/customvisiontraining/publishiteration/publishiteration

            using (var client = new HttpClient())
            {
                var predictionId = $"/subscriptions/{azureResourceSubscriptionId}/resourceGroups/{azureResourceGroupName}/providers/Microsoft.CognitiveServices/accounts/{cognitiveServiceResourceName}";
                client.DefaultRequestHeaders.Add("Training-Key", apiKey);
                var result = await client.PostAsync(
                    $"{resourceBaseEndpoint}/customvision/v3.0/training/projects/{projectId}/iterations/{iterationId}/publish?publishName={publishName}&predictionId={predictionId}", null);

                return result.IsSuccessStatusCode;
            }
        }

        /// <summary>
        /// Delete a training iteration by the given id.
        /// </summary>
        /// <param name="iterationId">Id of the training iteration to delete.</param>
        /// <returns>Deletion success result.</returns>
        public async Task<bool> DeleteTrainingIteration(string iterationId)
        {
            // https://docs.microsoft.com/en-us/rest/api/cognitiveservices/customvisiontraining/unpublishiteration/unpublishiteration
            // https://docs.microsoft.com/en-us/rest/api/cognitiveservices/customvisiontraining/deleteiteration/deleteiteration
            
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Training-Key", apiKey);
                // before deleting the training iteration must be deleted
                await client.DeleteAsync($"{resourceBaseEndpoint}/customvision/v3.0/training/projects/{projectId}/iterations/{iterationId}/publish");
                var result = await client.DeleteAsync($"{resourceBaseEndpoint}/customvision/v3.0/training/projects/{projectId}/iterations/{iterationId}");

                return result.IsSuccessStatusCode;
            }
        }

        /// <summary>
        /// Get classification info for a provided image.
        /// </summary>
        /// <param name="image">Image data</param>
        /// <param name="publishedName">Published name of the trained model.</param>
        /// <returns>Classification information-</returns>
        public async Task<ImagePredictionResult> DetectImage(byte[] image, string publishedName)
        {
            // https://docs.microsoft.com/en-us/rest/api/cognitiveservices/customvisionprediction/classifyimage/classifyimage

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Prediction-Key", apiPredictionKey);
                var result = await client.PostAsync(
                    $"{resourceBasePredictionEndpoint}/customvision/v3.0/prediction/{projectId}/classify/iterations/{publishedName}/image",
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
