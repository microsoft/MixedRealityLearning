using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;
using TrackedObjectsService.Data;

namespace TrackedObjectsService
{
    public static class DataService
    {
        private static CloudTableClient CloudTableClient;
        private static CloudTable TrackedObjectsTable;
        private static string PartitionKey;

        static DataService()
        {
            PartitionKey = Environment.GetEnvironmentVariable("PartitionKey");
            var connectionString = Environment.GetEnvironmentVariable("AzureStorageConnectionString");
            var storageAccount = CloudStorageAccount.Parse(connectionString);
            CloudTableClient = storageAccount.CreateCloudTableClient();
            var trackedObjectsTableName = Environment.GetEnvironmentVariable("TrackedObjectsTableName");
            TrackedObjectsTable = CloudTableClient.GetTableReference(trackedObjectsTableName);
        }

        public static async Task<int> CountAllTrackedObjects()
        {
            var query = new TableQuery();
            var segment = await TrackedObjectsTable.ExecuteQuerySegmentedAsync(query, null);
            
            return segment.Results.Count;
        }

        public static async Task<FindTrackedObjectResponse> Find(string trackedObjectName)
        {
            var query = new TableQuery().Where(
                TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, PartitionKey),
                    TableOperators.And,
                    TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, trackedObjectName)));
            var segment = await TrackedObjectsTable.ExecuteQuerySegmentedAsync(query, null);
            var trackedObjectFromDb = segment.Results.FirstOrDefault();

            return FindTrackedObjectResponse.CreateFromDynamicTableEntity(trackedObjectFromDb);
        }
    }
}
