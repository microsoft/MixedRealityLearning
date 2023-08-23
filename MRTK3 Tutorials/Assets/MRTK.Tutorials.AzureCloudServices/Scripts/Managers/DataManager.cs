// Copyright (c) Microsoft Corporation. 
// Licensed under the MIT License.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;
using MRTK.Tutorials.AzureCloudServices.Scripts.Domain;
using UnityEngine;
using UnityEngine.Events;

namespace MRTK.Tutorials.AzureCloudServices.Scripts.Managers
{
    public class DataManager : MonoBehaviour
    {
        public bool IsReady { get; private set; }
        
        [Header("Base Settings")]
        [SerializeField]
        private string connectionString = default;
        [SerializeField]
        private string projectName = "MyAzurePowerToolsProject";
        [Header("Table Settings")]
        [SerializeField]
        private string projectsTableName = "projects";
        [SerializeField]
        private string trackedObjectsTableName = "objects";
        [SerializeField]
        private string partitionKey = "main";
        [SerializeField]
        private bool tryCreateTableOnStart = true;
        [Header("Blob Settings")]
        [SerializeField]
        private string blockBlobContainerName = "tracked-objects-thumbnails";
        [SerializeField]
        private bool tryCreateBlobContainerOnStart = true;
        [Header("Events")]
        [SerializeField]
        private UnityEvent onDataManagerReady = default;
        [SerializeField]
        private UnityEvent onDataManagerInitFailed = default;

        private CloudStorageAccount storageAccount;
        private CloudTableClient cloudTableClient;
        private CloudTable projectsTable;
        private CloudTable trackedObjectsTable;
        private CloudBlobClient blobClient;
        private CloudBlobContainer blobContainer;

        private async void Awake()
        {
           
            storageAccount = CloudStorageAccount.Parse(connectionString);
            cloudTableClient = storageAccount.CreateCloudTableClient();
            projectsTable = cloudTableClient.GetTableReference(projectsTableName);
            trackedObjectsTable = cloudTableClient.GetTableReference(trackedObjectsTableName);
            if (tryCreateTableOnStart)
            {
                try
                {
                    if (await projectsTable.CreateIfNotExistsAsync())
                    {
                        Debug.Log($"Created table {projectsTableName}.");
                    }
                    if (await trackedObjectsTable.CreateIfNotExistsAsync())
                    {
                        Debug.Log($"Created table {trackedObjectsTableName}.");
                    }
                }
                catch (StorageException ex)
                {
                    Debug.LogError("Failed to connect with Azure Storage.\nIf you are running with the default storage emulator configuration, please make sure you have started the storage emulator.");
                    Debug.LogException(ex);
                    onDataManagerInitFailed?.Invoke();
                }
            }

            blobClient = storageAccount.CreateCloudBlobClient();
            blobContainer = blobClient.GetContainerReference(blockBlobContainerName);
            if (tryCreateBlobContainerOnStart)
            {
                try
                {
                    if (await blobContainer.CreateIfNotExistsAsync())
                    {
                        Debug.Log($"Created container {blockBlobContainerName}.");
                    }
                }
                catch (StorageException ex)
                {
                    Debug.LogError("Failed to connect with Azure Storage.\nIf you are running with the default storage emulator configuration, please make sure you have started the storage emulator.");
                    Debug.LogException(ex);
                    onDataManagerInitFailed?.Invoke();
                }
            }

            IsReady = true;
            onDataManagerReady?.Invoke();
        }

        /// <summary>
        /// Get a project or create one if it does not exist.
        /// </summary>
        /// <returns>Project instance from database.</returns>
        public async Task<Project> GetOrCreateProject()
        {
            var query = new TableQuery<Project>().Where(
                TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionKey),
                    TableOperators.And,
                    TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, projectName)));
            var segment = await projectsTable.ExecuteQuerySegmentedAsync(query, null);

            var project = segment.Results.FirstOrDefault();
            if (project != null)
            {
                return project;
            }

            project = new Project()
            {
                Name = projectName,
                RowKey = projectName,
                PartitionKey = partitionKey,
                CustomVisionIterationId = string.Empty,
                CustomVisionPublishedModelName = string.Empty
            };
            
            var insertOrMergeOperation = TableOperation.InsertOrMerge(project);
            await projectsTable.ExecuteAsync(insertOrMergeOperation);

            return project;
        }

        /// <summary>
        /// Update the project changes back to the table store;
        /// </summary>
        public async Task<bool> UpdateProject(Project project)
        {
            var insertOrMergeOperation = TableOperation.InsertOrMerge(project);
            var result = await projectsTable.ExecuteAsync(insertOrMergeOperation);

            return result.Result != null;
        }

        /// <summary>
        /// Insert a new or update an TrackedObjectProject instance on the table storage.
        /// </summary>
        /// <param name="trackedObject">Instance to write or update.</param>
        /// <returns>Success result.</returns>
        public async Task<bool> UploadOrUpdate(TrackedObject trackedObject)
        {
            if (string.IsNullOrWhiteSpace(trackedObject.PartitionKey))
            {
                trackedObject.PartitionKey = partitionKey;
            }
            
            var insertOrMergeOperation = TableOperation.InsertOrMerge(trackedObject);
            var result = await trackedObjectsTable.ExecuteAsync(insertOrMergeOperation);

            return result.Result != null;
        }

        /// <summary>
        /// Get all TrackedObjectProjects from the table.
        /// </summary>
        /// <returns>List of all TrackedObjectProjects from table.</returns>
        public async Task<List<TrackedObject>> GetAllTrackedObjects()
        {
            var query = new TableQuery<TrackedObject>();
            var segment = await trackedObjectsTable.ExecuteQuerySegmentedAsync(query, null);

            return segment.Results;
        }

        /// <summary>
        /// Find a TrackedObjectProject by a given Id (partition key).
        /// </summary>
        /// <param name="id">Id/Partition Key to search by.</param>
        /// <returns>Found TrackedObjectProject, null if nothing is found.</returns>
        public async Task<TrackedObject> FindTrackedObjectById(string id)
        {
            var retrieveOperation = TableOperation.Retrieve<TrackedObject>(partitionKey, id);
            var result = await trackedObjectsTable.ExecuteAsync(retrieveOperation);
            var trackedObject = result.Result as TrackedObject;

            return trackedObject;
        }

        /// <summary>
        /// Find a TrackedObjectProject by its name.
        /// </summary>
        /// <param name="trackedObjectName">Name to search by.</param>
        /// <returns>Found TrackedObjectProject, null if nothing is found.</returns>
        public async Task<TrackedObject> FindTrackedObjectByName(string trackedObjectName)
        {
            var query = new TableQuery<TrackedObject>().Where(
                TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionKey),
                    TableOperators.And,
                    TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, trackedObjectName)));
            var segment = await trackedObjectsTable.ExecuteQuerySegmentedAsync(query, null);

            return segment.Results.FirstOrDefault();
        }

        /// <summary>
        /// Delete a TrackedObjectProject from the table.
        /// </summary>
        /// <param name="instance">Object to delete.</param>
        /// <returns>Success result of deletion.</returns>
        public async Task<bool> DeleteTrackedObject(TrackedObject instance)
        {
            var deleteOperation = TableOperation.Delete(instance);
            var result = await trackedObjectsTable.ExecuteAsync(deleteOperation);

            return result.HttpStatusCode == (int)HttpStatusCode.OK;
        }

        /// <summary>
        /// Upload data to a blob.
        /// </summary>
        /// <param name="data">Data to upload.</param>
        /// <param name="blobName">Name of the blob, ideally with file type.</param>
        /// <returns>Uri to the blob.</returns>
        public async Task<string> UploadBlob(byte[] data, string blobName)
        {
            var blockBlob = blobContainer.GetBlockBlobReference(blobName);
            await blockBlob.UploadFromByteArrayAsync(data, 0, data.Length);

            return blockBlob.Name;
        }

        /// <summary>
        /// Download data from a blob.
        /// </summary>
        /// <param name="blobName">Name of the blob.</param>
        /// <returns>Data as byte array.</returns>
        public async Task<byte[]> DownloadBlob(string blobName)
        {
            var blockBlob = blobContainer.GetBlockBlobReference(blobName);
            using (var stream = new MemoryStream())
            {
                await blockBlob.DownloadToStreamAsync(stream);
                return stream.ToArray();
            }
        }

        /// <summary>
        /// Delete a blob if it exists.
        /// </summary>
        /// <param name="blobName">Name of the blob to delete.</param>
        /// <returns>Success result of deletion.</returns>
        public async Task<bool> DeleteBlob(string blobName)
        {
            var blockBlob = blobContainer.GetBlockBlobReference(blobName);
            return await blockBlob.DeleteIfExistsAsync();
        }
    }
}
