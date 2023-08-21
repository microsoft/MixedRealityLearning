// Copyright (c) Microsoft Corporation. 
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using UnityEngine;

namespace MRTK.Tutorials.AzureCloudServices.Scripts.Utilities
{
    public class DebugBlobWriter : MonoBehaviour
    {
        [SerializeField]
        private string connectionString = "UseDevelopmentStorage=true";
        [SerializeField]
        private string blobContainerName = "debug-logs";
        [SerializeField]
        private string filePrefix = "unityapp";
        [SerializeField]
        private bool tryCreateBlobContainerOnStart = true;
        
        private CloudStorageAccount storageAccount;
        private CloudBlobClient blobClient;
        private CloudBlobContainer blobContainer;
        private CloudAppendBlob fileBlobReference;
        
        private Queue<string> messages = new Queue<string>();

        private async void Start()
        {
            storageAccount = CloudStorageAccount.Parse(connectionString);
            blobClient = storageAccount.CreateCloudBlobClient();
            blobContainer = blobClient.GetContainerReference(blobContainerName);
            if (tryCreateBlobContainerOnStart)
            {
                try
                {
                    if (await blobContainer.CreateIfNotExistsAsync())
                    {
                        Debug.Log($"Created container {blobContainerName}.");
                    }
                }
                catch (StorageException ex)
                {
                    Debug.LogError("Failed to connect with Azure Storage.\nIf you are running with the default storage emulator configuration, please make sure you have started the storage emulator.");
                    Debug.LogException(ex);
                }
            }

            var fileName = $"{filePrefix}_{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.txt";
            fileBlobReference = blobContainer.GetAppendBlobReference(fileName);
            await fileBlobReference.CreateOrReplaceAsync();
            Application.logMessageReceived += HandleOnlogMessageReceived;
            StartCoroutine(CheckLogsToWriteCoroutine());
        }
        
        private void OnDestroy()
        {
            StopAllCoroutines();
            Application.logMessageReceived -= HandleOnlogMessageReceived;
        }

        private IEnumerator CheckLogsToWriteCoroutine()
        {
            var waiter = new WaitForSeconds(2f);
            while (gameObject.activeSelf)
            {
                yield return waiter;
                if (messages.Count == 0)
                {
                    continue;
                }
                
                var messageToWrite = new StringBuilder();
                while (messages.Count > 0)
                {
                    messageToWrite.Append(messages.Dequeue());
                }
                WriteMessages(messageToWrite.ToString());
            }
        }

        private async void WriteMessages(string message)
        {
            await fileBlobReference.AppendTextAsync(message);
        }

        private void HandleOnlogMessageReceived(string message, string stacktrace, LogType type)
        {
            messages.Enqueue($"[{DateTime.Now:HH:mm:ss}] {type}: {message}\n");
        }
    }
}
