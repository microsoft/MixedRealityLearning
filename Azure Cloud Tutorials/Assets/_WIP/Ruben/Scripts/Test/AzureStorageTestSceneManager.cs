using System;
using System.Collections.Generic;
using System.Text;
using MRTK.Tutorials.AzureCloudPower.Domain;
using MRTK.Tutorials.AzureCloudPower.Managers;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace MRTK.Tutorials.AzureCloudPower.Test
{
    public class AzureStorageTestSceneManager : MonoBehaviour
    {
        [SerializeField]
        private DataManager dataManager;
        [SerializeField]
        private Text text;

        public async void UploadToTable()
        {
            var dummyTrackedObject = CreateDummy();
            text.text = $"Uploading TrackedObject '{dummyTrackedObject.Id}' to Table.";
            var success = await dataManager.UploadOrUpdate(dummyTrackedObject);
            if (success)
            {
                text.text = $"Uploaded TrackedObject\n\tId: {dummyTrackedObject.Id}\n\tName: {dummyTrackedObject.Name}";
            }
            else
            {
                text.text = $"Failed to upload TrackedObject '{dummyTrackedObject.Id}' to table.";
            }
        }

        public async void ListFromTable()
        {
            text.text = "Fetching all TrackedObjects from table.";
            
            var objects = await dataManager.GetAll();
            var textToWrite = new StringBuilder();
            textToWrite.Append($"Found objects ({objects.Count})\n");
            foreach (var trackedObject in objects)
            {
                textToWrite.AppendLine($"-\tId: {trackedObject.Id} Name: {trackedObject.Name}");
            }

            text.text = textToWrite.ToString();
        }

        public async void DeleteAllFromTable()
        {
            text.text = "Checking if TrackedObject to delete exists.";
            var objects = await dataManager.GetAll();
            if (objects.Count == 0)
            {
                text.text = "The table is empty, there is nothing to delete.";
                return;
            }

            text.text = $"Starting to delete {objects.Count} TrackedObjects...";

            foreach (var trackedObject in objects)
            {
                await dataManager.Delete(trackedObject);
            }

            text.text = "All TrackedObjects have been deleted!";
        }

        private ObjectProject CreateDummy()
        {
            var id = Guid.NewGuid()
                .ToString()
                .Replace("-", "")
                .Substring(0, 6);

            return new ObjectProject
            {
                Id = id,
                RowKey = id,
                Name = $"Name_{Random.Range(1000, 9999)}",
                ThumbnailBlobUrl = $"{Random.Range(1000, 9999)}_img.png",
                SpatialAnchorId = $"spatial-id_{Random.Range(1000, 9999)}"
            };
        }
    }
}