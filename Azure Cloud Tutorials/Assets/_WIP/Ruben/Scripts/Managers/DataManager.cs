using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;
using MRTK.Tutorials.AzureCloudPower.Domain;
using UnityEngine;

namespace MRTK.Tutorials.AzureCloudPower.Managers
{
    public class DataManager : MonoBehaviour
    {
        [Header("Azure Storage Base Settings")]
        [SerializeField]
        private string connectionString = "UseDevelopmentStorage=true";
        [Header("Table Settings")]
        [SerializeField]
        private string tableName = "trackedobjects";
        [SerializeField]
        private string partitionKey = "main";
        [SerializeField]
        private bool tryCreateTableOnStart = true;
        [Header("Blob Settings")]
        [SerializeField]
        private string blockBlobContainerName = "trackedobjectslob";
        [SerializeField]
        private bool tryCreateBlobContainerOnStart = true;

        private CloudStorageAccount storageAccount;
        private CloudTableClient cloudTableClient;
        private CloudTable trackedObjectsTable;
        private CloudBlobClient blobClient;
        private CloudBlobContainer blobContainer;

        private async void Awake()
        {
            storageAccount = CloudStorageAccount.Parse(connectionString);
            cloudTableClient = storageAccount.CreateCloudTableClient();
            trackedObjectsTable = cloudTableClient.GetTableReference(tableName);
            if (tryCreateTableOnStart)
            {
                try
                {
                    if (await trackedObjectsTable.CreateIfNotExistsAsync())
                    {
                        Debug.Log($"Created table {tableName}.");
                    }
                }
                catch (StorageException ex)
                {
                    Debug.LogError("Failed to connect with Azure Storage.\nIf you are running with the default storage emulator configuration, please make sure you have started the storage emulator.");
                    Debug.LogException(ex);
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
                }
            }
        }

        /// <summary>
        /// Write or update a TrackedObject instance to the table storage.
        /// </summary>
        /// <param name="trackedObject">Instance to write or update.</param>
        /// <returns>Success result.</returns>
        public async Task<bool> WriteTrackedObject(TrackedObject trackedObject)
        {
            var insertOrMergeOperation = TableOperation.InsertOrMerge(trackedObject);
            var result = await trackedObjectsTable.ExecuteAsync(insertOrMergeOperation);

            return result.HttpStatusCode == (int)HttpStatusCode.OK;
        }

        /// <summary>
        /// Get all TrackObject from the table.
        /// </summary>
        /// <returns>List of TrackedObject from table.</returns>
        public async Task<List<TrackedObject>> GetAllTrackedObjects()
        {
            var query = new TableQuery<TrackedObject>();
            var segment = await trackedObjectsTable.ExecuteQuerySegmentedAsync(query, null);

            return segment.Results;
        }

        /// <summary>
        /// Find a TrackedObject by a given Id (partion key).
        /// </summary>
        /// <param name="id">Id/Partition Key to search by.</param>
        /// <returns>Found TrackedObject, null if nothing is found.</returns>
        public async Task<TrackedObject> FindTrackedObjectById(string id)
        {
            var retrieveOperation = TableOperation.Retrieve<TrackedObject>(partitionKey, id);
            var result = await trackedObjectsTable.ExecuteAsync(retrieveOperation);
            var trackedObject = result.Result as TrackedObject;

            return trackedObject;
        }

        /// <summary>
        /// Find a TrackedObject by its name.
        /// </summary>
        /// <param name="name">Name to search by.</param>
        /// <returns>Found TrackedObject, null if nothing is found.</returns>
        public async Task<TrackedObject> FindTrackedObjectByName(string name)
        {
            var query = new TableQuery<TrackedObject>().Where(
                TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionKey),
                    TableOperators.And,
                    TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, name)));
            var segment = await trackedObjectsTable.ExecuteQuerySegmentedAsync(query, null);

            return segment.Results.FirstOrDefault();
        }

        /// <summary>
        /// Delete a TrackedObject from the table.
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

            return blockBlob.Uri.ToString();
        }

        /// <summary>
        /// Download data from a blob.
        /// </summary>
        /// <param name="blobName">Name of the blob.</param>
        /// <returns>Data as byte array.</returns>
        public async Task<byte[]> DownloadBlob(string blobName)
        {
            var blockBlob = blobContainer.GetBlockBlobReference(blobName);
            await blockBlob.FetchAttributesAsync();
            var data = new byte[blockBlob.Properties.Length];
            await blockBlob.DownloadToByteArrayAsync(data, data.Length);

            return data;
        }

        /// <summary>
        /// Delete a blob if it exists.
        /// </summary>
        /// <param name="blobName">Name of the blob to delete.</param>
        /// <returns>Success result of deletion.</returns>
        public async Task<bool> DeleteBlob(string blobName)
        {
            var blockBlob = blobContainer.GetBlockBlobReference(blobName);
            await blockBlob.DeleteIfExistsAsync();

            return blockBlob.IsDeleted;
        }
    }
}
