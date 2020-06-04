using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
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
    }
}
