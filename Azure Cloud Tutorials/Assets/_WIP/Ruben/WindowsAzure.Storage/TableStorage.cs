using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;
using MRTK.Tutorials.AzureCloudPower.Domain;

namespace Microsoft.WindowsAzure.Storage
{
    public class TableStorage : BaseStorage
    {
        public string TableName = "people";

        public string DemoShare = "demofileshare";
        public string DemoDirectory = "demofiledirectory";
        public string QueueName = "samplequeue";

        public void TestWringTrackedObjectInfo()
        {
            TableStorageTest();
        }

        public async void TableStorageTest()
        {
            ClearOutput();
            WriteLine("-- Testing Table Storage --");

            WriteLine("0. Creating table client");
            CloudTableClient tableClient = StorageAccount.CreateCloudTableClient();

            WriteLine("1. Create a Table for the demo");

            // Create a table client for interacting with the table service 
            CloudTable table = tableClient.GetTableReference(TableName);

            try
            {
                if (await table.CreateIfNotExistsAsync())
                {
                    WriteLine(string.Format("Created Table named: {0}", TableName));
                }
                else
                {

                    WriteLine(string.Format("Table {0} already exists", TableName));
                }
            }
            catch (StorageException)
            {
                WriteLine("If you are running with the default configuration please make sure you have started the storage emulator. Press the Windows key and type Azure Storage to select and run it from the list of applications - then restart the sample.");
                throw;
            }

            // Create an instance of a customer entity. See the Model\CustomerEntity.cs for a description of the entity.
            CustomerEntity customer = new CustomerEntity("Harp", "Walter")
            {
                Email = "Walter@contoso.com",
                PhoneNumber = "425-555-0101"
            };

            // Demonstrate how to Update the entity by changing the phone number
            WriteLine("2. Update an existing Entity using the InsertOrMerge Operation.");
            customer.PhoneNumber = "425-555-0105";
            await InsertOrMergeEntityAsync(table, customer);

            // Demonstrate how to Read the updated entity using a point query 
            WriteLine("3. Reading the updated Entity.");
            customer = await RetrieveEntityUsingPointQueryAsync(table, "Harp", "Walter");

            // Demonstrate how to Delete an entity
            WriteLine("4. Delete the entity.");
            await DeleteEntityAsync(table, customer);

            // Demonstrate upsert and batch table operations
            WriteLine("5. Inserting a batch of entities.");
            await BatchInsertOfCustomerEntitiesAsync(table);

            // Query a range of data within a partition
            WriteLine("6. Retrieving entities with surname of Smith and first names >= 1 and <= 5");
            await PartitionRangeQueryAsync(table, "Smith", "0001", "0005");

            // Query for all the data within a partition 
            WriteLine("7. Retrieve entities with surname of Smith.");
            await PartitionScanAsync(table, "Smith");

            WriteLine("-- Test Complete --");
        }

        private async Task<CustomerEntity> InsertOrMergeEntityAsync(CloudTable table, CustomerEntity entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException("entity");
            }

            // Create the InsertOrReplace  TableOperation
            TableOperation insertOrMergeOperation = TableOperation.InsertOrMerge(entity);

            // Execute the operation.
            TableResult result = await table.ExecuteAsync(insertOrMergeOperation);
            CustomerEntity insertedCustomer = result.Result as CustomerEntity;
            return insertedCustomer;
        }

        private async Task<CustomerEntity> RetrieveEntityUsingPointQueryAsync(CloudTable table, string partitionKey, string rowKey)
        {
            TableOperation retrieveOperation = TableOperation.Retrieve<CustomerEntity>(partitionKey, rowKey);
            TableResult result = await table.ExecuteAsync(retrieveOperation);
            CustomerEntity customer = result.Result as CustomerEntity;
            if (customer != null)
            {
                WriteLine(string.Format("\t{0}\t{1}\t{2}\t{3}", customer.PartitionKey, customer.RowKey, customer.Email, customer.PhoneNumber));
            }

            return customer;
        }

        private async Task DeleteEntityAsync(CloudTable table, CustomerEntity deleteEntity)
        {
            if (deleteEntity == null)
            {
                throw new ArgumentNullException("deleteEntity");
            }

            TableOperation deleteOperation = TableOperation.Delete(deleteEntity);
            await table.ExecuteAsync(deleteOperation);
        }

        private async Task BatchInsertOfCustomerEntitiesAsync(CloudTable table)
        {
            // Create the batch operation. 
            TableBatchOperation batchOperation = new TableBatchOperation();

            // The following code  generates test data for use during the query samples.  
            for (int i = 0; i < 10; i++)
            {
                batchOperation.InsertOrMerge(new CustomerEntity("Smith", string.Format("{0}", i.ToString("D4")))
                {
                    Email = string.Format("{0}@contoso.com", i.ToString("D4")),
                    PhoneNumber = string.Format("425-555-{0}", i.ToString("D4"))
                });
            }

            // Execute the batch operation.
            IList<TableResult> results = await table.ExecuteBatchAsync(batchOperation);
            foreach (var res in results)
            {
                var customerInserted = res.Result as CustomerEntity;
                WriteLine(string.Format("Inserted entity with\t Etag = {0} and PartitionKey = {1}, RowKey = {2}", customerInserted.ETag, customerInserted.PartitionKey, customerInserted.RowKey));
            }

        }

        private async Task PartitionRangeQueryAsync(CloudTable table, string partitionKey, string startRowKey, string endRowKey)
        {
            // Create the range query using the fluid API 
            TableQuery<CustomerEntity> rangeQuery = new TableQuery<CustomerEntity>().Where(
                TableQuery.CombineFilters(
                        TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionKey),
                        TableOperators.And,
                        TableQuery.CombineFilters(
                            TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.GreaterThanOrEqual, startRowKey),
                            TableOperators.And,
                            TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.LessThanOrEqual, endRowKey))));

            // Page through the results - requesting 50 results at a time from the server. 
            TableContinuationToken token = null;
            rangeQuery.TakeCount = 50;
            do
            {
                TableQuerySegment<CustomerEntity> segment = await table.ExecuteQuerySegmentedAsync(rangeQuery, token);
                token = segment.ContinuationToken;
                foreach (CustomerEntity entity in segment)
                {
                    WriteLine(string.Format("Customer: {0},{1}\t{2}\t{3}", entity.PartitionKey, entity.RowKey, entity.Email, entity.PhoneNumber));
                }
            }
            while (token != null);
        }

        private async Task PartitionScanAsync(CloudTable table, string partitionKey)
        {
            TableQuery<CustomerEntity> partitionScanQuery = new TableQuery<CustomerEntity>().Where
                (TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionKey));

            TableContinuationToken token = null;
            // Page through the results
            do
            {
                TableQuerySegment<CustomerEntity> segment = await table.ExecuteQuerySegmentedAsync(partitionScanQuery, token);
                token = segment.ContinuationToken;
                foreach (CustomerEntity entity in segment)
                {
                    WriteLine(string.Format("Customer: {0},{1}\t{2}\t{3}", entity.PartitionKey, entity.RowKey, entity.Email, entity.PhoneNumber));
                }
            }
            while (token != null);
        }
    }

    public class CustomerEntity : TableEntity
    {
        // Your entity type must expose a parameter-less constructor
        public CustomerEntity() { }

        // Define the PK and RK
        public CustomerEntity(string lastName, string firstName)
        {
            this.PartitionKey = lastName;
            this.RowKey = firstName;
        }

        //For any property that should be stored in the table service, the property must be a public property of a supported type that exposes both get and set.        
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
    } 
}