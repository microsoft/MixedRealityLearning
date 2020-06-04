using System;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Queue;

namespace Microsoft.WindowsAzure.Storage
{
    public class QueueStorage : BaseStorage
    {
        public async void QueueStorageTest()
        {
            ClearOutput();
            WriteLine("-- Testing Queue Storage --");

            WriteLine("0. Creating queue client");
            // Create a queue client for interacting with the queue service
            CloudQueueClient queueClient = StorageAccount.CreateCloudQueueClient();

            WriteLine("1. Create a queue for the demo");
            CloudQueue queue = queueClient.GetQueueReference("samplequeue");
            try
            {
                await queue.CreateIfNotExistsAsync();
            }
            catch (StorageException)
            {
                WriteLine("If you are running with the default configuration please make sure you have started the storage emulator. Press the Windows key and type Azure Storage to select and run it from the list of applications - then restart the sample.");
                throw;
            }

            // Demonstrate basic queue functionality 
            await BasicQueueOperationsAsync(queue);

            // Demonstrate how to update an enqueued message
            await UpdateEnqueuedMessageAsync(queue);

            // Demonstrate advanced functionality such as processing of batches of messages
            await ProcessBatchOfMessagesAsync(queue);

            // When you delete a queue it could take several seconds before you can recreate a queue with the same
            // name - hence to enable you to run the demo in quick succession the queue is not deleted. If you want 
            // to delete the queue uncomment the line of code below. 
            await DeleteQueueAsync(queue);

            WriteLine("-- Test Complete --");
        }

        /// <summary>
        /// Demonstrate basic queue operations such as adding a message to a queue, peeking at the front of the queue and dequeing a message.
        /// </summary>
        /// <param name="queue">The sample queue</param>
        private async Task BasicQueueOperationsAsync(CloudQueue queue)
        {
            // Insert a message into the queue using the AddMessage method. 
            WriteLine("2. Insert a single message into a queue");
            await queue.AddMessageAsync(new CloudQueueMessage("Hello World!"));

            // Peek at the message in the front of a queue without removing it from the queue using PeekMessage (PeekMessages lets you peek >1 message)
            WriteLine("3. Peek at the next message");
            CloudQueueMessage peekedMessage = await queue.PeekMessageAsync();
            if (peekedMessage != null)
            {
                WriteLine(string.Format("The peeked message is: {0}", peekedMessage.AsString));
            }

            // You de-queue a message in two steps. Call GetMessage at which point the message becomes invisible to any other code reading messages 
            // from this queue for a default period of 30 seconds. To finish removing the message from the queue, you call DeleteMessage. 
            // This two-step process ensures that if your code fails to process a message due to hardware or software failure, another instance 
            // of your code can get the same message and try again. 
            WriteLine("4. De-queue the next message");
            CloudQueueMessage message = await queue.GetMessageAsync();
            if (message != null)
            {
                WriteLine(string.Format("Processing & deleting message with content: {0}", message.AsString));
                await queue.DeleteMessageAsync(message);
            }
        }

        /// <summary>
        /// Update an enqueued message and its visibility time. For workflow scenarios this could enable you to update 
        /// the status of a task as well as extend the visibility timeout in order to provide more time for a client 
        /// continue working on the message before another client can see the message. 
        /// </summary>
        /// <param name="queue">The sample queue</param>
        private async Task UpdateEnqueuedMessageAsync(CloudQueue queue)
        {
            // Insert another test message into the queue 
            WriteLine("5. Insert another test message ");
            await queue.AddMessageAsync(new CloudQueueMessage("Hello World Again!"));

            WriteLine("6. Change the contents of a queued message");
            CloudQueueMessage message = await queue.GetMessageAsync();
            message.SetMessageContent("Updated contents.");
            await queue.UpdateMessageAsync(
                message,
                TimeSpan.Zero,  // For the purpose of the sample make the update visible immediately
                MessageUpdateFields.Content |
                MessageUpdateFields.Visibility);
        }

        /// <summary>
        /// Demonstrate adding a number of messages, checking the message count and batch retrieval of messages. During retrieval we 
        /// also set the visibility timeout to 5 minutes. Visibility timeout is the amount of time message is not visible to other 
        /// clients after a GetMessageOperation assuming DeleteMessage is not called. 
        /// </summary>
        /// <param name="queue">The sample queue</param>
        private async Task ProcessBatchOfMessagesAsync(CloudQueue queue)
        {
            // Enqueue 20 messages by which to demonstrate batch retrieval
            WriteLine("7. Enqueue 20 messages.");
            for (int i = 0; i < 20; i++)
            {
                await queue.AddMessageAsync(new CloudQueueMessage(string.Format("{0} - {1}", i, "Hello World")));
            }

            // The FetchAttributes method asks the Queue service to retrieve the queue attributes, including an approximation of message count 
            WriteLine("8. Get the queue length");
            await queue.FetchAttributesAsync();
            int? cachedMessageCount = queue.ApproximateMessageCount;
            WriteLine(string.Format("Number of messages in queue: {0}", cachedMessageCount));

            // Dequeue a batch of 21 messages (up to 32) and set visibility timeout to 5 minutes. Note we are dequeuing 21 messages because the earlier
            // UpdateEnqueuedMessage method left a message on the queue hence we are retrieving that as well. 
            WriteLine("9. Dequeue 21 messages, allowing 5 minutes for the clients to process.");
            foreach (CloudQueueMessage msg in await queue.GetMessagesAsync(21, TimeSpan.FromMinutes(5), null, null))
            {
                WriteLine(string.Format("Processing & deleting message with content: {0}", msg.AsString));

                // Process all messages in less than 5 minutes, deleting each message after processing.
                await queue.DeleteMessageAsync(msg);
            }
        }

        /// <summary>
        /// Validate the connection string information in app.config and throws an exception if it looks like 
        /// the user hasn't updated this to valid values. 
        /// </summary>
        /// <param name="storageConnectionString">The storage connection string</param>
        /// <returns>CloudStorageAccount object</returns>
        private CloudStorageAccount CreateStorageAccountFromConnectionString(string storageConnectionString)
        {
            CloudStorageAccount storageAccount;
            try
            {
                storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            }
            catch (FormatException)
            {
                WriteLine("Invalid storage account information provided. Please confirm the AccountName and AccountKey are valid in the app.config file - then restart the sample.");
                throw;
            }
            catch (ArgumentException)
            {
                WriteLine("Invalid storage account information provided. Please confirm the AccountName and AccountKey are valid in the app.config file - then restart the sample.");
                throw;
            }

            return storageAccount;
        }

        /// <summary>
        /// Delete the queue that was created for this sample
        /// </summary>
        /// <param name="queue">The sample queue to delete</param>
        private async Task DeleteQueueAsync(CloudQueue queue)
        {
            WriteLine("10. Delete the queue");
            await queue.DeleteAsync();
        }
    } 
}
