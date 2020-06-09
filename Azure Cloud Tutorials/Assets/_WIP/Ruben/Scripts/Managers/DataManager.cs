using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;
using MRTK.Tutorials.AzureCloudPower.Domain;
using UnityEngine;
using UnityEngine.Events;

namespace MRTK.Tutorials.AzureCloudPower.Managers
{
    public class DataManager : MonoBehaviour
    {
        public bool IsReady { get; private set; }
        
        [Header("Azure Storage Base Settings")]
        [SerializeField]
        private string connectionString = "UseDevelopmentStorage=true";
        [Header("Table Settings")]
        [SerializeField]
        private string tableName = "ObjectProjects";
        [SerializeField]
        private string partitionKey = "main";
        [SerializeField]
        private bool tryCreateTableOnStart = true;
        [Header("Blob Settings")]
        [SerializeField]
        private string blockBlobContainerName = "ObjectProjectsBlob";
        [SerializeField]
        private bool tryCreateBlobContainerOnStart = true;
        [Header("Events")]
        [SerializeField]
        private UnityEvent onDataManagerReady;
        [SerializeField]
        private UnityEvent onDataManagerInitFailed;

        private CloudStorageAccount storageAccount;
        private CloudTableClient cloudTableClient;
        private CloudTable objectProjectsTable;
        private CloudBlobClient blobClient;
        private CloudBlobContainer blobContainer;

        private async void Awake()
        {
            storageAccount = CloudStorageAccount.Parse(connectionString);
            cloudTableClient = storageAccount.CreateCloudTableClient();
            objectProjectsTable = cloudTableClient.GetTableReference(tableName);
            if (tryCreateTableOnStart)
            {
                try
                {
                    if (await objectProjectsTable.CreateIfNotExistsAsync())
                    {
                        Debug.Log($"Created table {tableName}.");
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
        /// Write or update a ObjectProject instance to the table storage.
        /// </summary>
        /// <param name="objectProject">Instance to write or update.</param>
        /// <returns>Success result.</returns>
        public async Task<bool> UploadOrUpdate(ObjectProject objectProject)
        {
            if (string.IsNullOrWhiteSpace(objectProject.PartitionKey))
            {
                objectProject.PartitionKey = partitionKey;
            }

            var insertOrMergeOperation = TableOperation.InsertOrMerge(objectProject);
            var result = await objectProjectsTable.ExecuteAsync(insertOrMergeOperation);

            return result.Result != null;
        }

        /// <summary>
        /// Get all ObjectProject from the table.
        /// </summary>
        /// <returns>List of ObjectProject from table.</returns>
        public async Task<List<ObjectProject>> GetAll()
        {
            var query = new TableQuery<ObjectProject>();
            var segment = await objectProjectsTable.ExecuteQuerySegmentedAsync(query, null);

            return segment.Results;
        }

        /// <summary>
        /// Find a ObjectProject by a given Id (partition key).
        /// </summary>
        /// <param name="id">Id/Partition Key to search by.</param>
        /// <returns>Found ObjectProject, null if nothing is found.</returns>
        public async Task<ObjectProject> FindById(string id)
        {
            var retrieveOperation = TableOperation.Retrieve<ObjectProject>(partitionKey, id);
            var result = await objectProjectsTable.ExecuteAsync(retrieveOperation);
            var trackedObject = result.Result as ObjectProject;

            return trackedObject;
        }

        /// <summary>
        /// Find a ObjectProject by its name.
        /// </summary>
        /// <param name="name">Name to search by.</param>
        /// <returns>Found ObjectProject, null if nothing is found.</returns>
        public async Task<ObjectProject> FindByName(string name)
        {
            var query = new TableQuery<ObjectProject>().Where(
                TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionKey),
                    TableOperators.And,
                    TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, name)));
            var segment = await objectProjectsTable.ExecuteQuerySegmentedAsync(query, null);

            return segment.Results.FirstOrDefault();
        }

        /// <summary>
        /// Delete a ObjectProject from the table.
        /// </summary>
        /// <param name="instance">Object to delete.</param>
        /// <returns>Success result of deletion.</returns>
        public async Task<bool> Delete(ObjectProject instance)
        {
            var deleteOperation = TableOperation.Delete(instance);
            var result = await objectProjectsTable.ExecuteAsync(deleteOperation);

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
