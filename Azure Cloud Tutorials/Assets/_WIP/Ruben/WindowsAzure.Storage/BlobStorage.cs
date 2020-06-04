using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Blob;
using UnityEngine;
using Random = System.Random;
#if WINDOWS_UWP && ENABLE_DOTNET
using Windows.Storage;
#endif

namespace Microsoft.WindowsAzure.Storage
{

    public class BlobStorage : BaseStorage
    {
        public string BlockBlobContainerName = "democontainerblockblob";
        public string PageBlobContainerName = "democontainerpageblob";

        public async void BlobStorageTest()
        {
            ClearOutput();
            WriteLine("-- Testing Blob Storage --");
            await BasicStorageBlockBlobOperationsAsync();
            await BasicStoragePageBlobOperationsAsync();
        }

        private async Task BasicStorageBlockBlobOperationsAsync()
        {
            WriteLine("Testing BlockBlob");

            const string ImageToUpload = "HelloWorld.png";

            // Create a blob client for interacting with the blob service.
            CloudBlobClient blobClient = StorageAccount.CreateCloudBlobClient();

            // Create a container for organizing blobs within the storage account.
            WriteLine("1. Creating Container");
            CloudBlobContainer container = blobClient.GetContainerReference("democontainerblockblob");
            try
            {
                await container.CreateIfNotExistsAsync();
            }
            catch (StorageException)
            {
                WriteLine("If you are running with the default configuration please make sure you have started the storage emulator. Press the Windows key and type Azure Storage to select and run it from the list of applications - then restart the sample.");
                throw;
            }

            // To view the uploaded blob in a browser, you have two options. The first option is to use a Shared Access Signature (SAS) token to delegate 
            // access to the resource. See the documentation links at the top for more information on SAS. The second approach is to set permissions 
            // to allow public access to blobs in this container. Uncomment the line below to use this approach. Then you can view the image 
            // using: https://[InsertYourStorageAccountNameHere].blob.core.windows.net/democontainer/HelloWorld.png
            // await container.SetPermissionsAsync(new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Blob });

            // Upload a BlockBlob to the newly created container
            WriteLine("2. Uploading BlockBlob");
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(ImageToUpload);

#if WINDOWS_UWP && ENABLE_DOTNET
		StorageFolder storageFolder = await StorageFolder.GetFolderFromPathAsync(Application.streamingAssetsPath.Replace('/', '\\'));
		StorageFile sf = await storageFolder.GetFileAsync(ImageToUpload);
		await blockBlob.UploadFromFileAsync(sf);
#else
            await blockBlob.UploadFromFileAsync(Path.Combine(Application.streamingAssetsPath, ImageToUpload));
#endif

            // List all the blobs in the container 
            WriteLine("3. List Blobs in Container");
            BlobContinuationToken token = null;
            BlobResultSegment list = await container.ListBlobsSegmentedAsync(token);
            foreach (IListBlobItem blob in list.Results)
            {
                // Blob type will be CloudBlockBlob, CloudPageBlob or CloudBlobDirectory
                // Use blob.GetType() and cast to appropriate type to gain access to properties specific to each type
                WriteLine(string.Format("- {0} (type: {1})", blob.Uri, blob.GetType()));
            }

            // Download a blob to your file system
            string path;
            WriteLine(string.Format("4. Download Blob from {0}", blockBlob.Uri.AbsoluteUri));
            string fileName = string.Format("CopyOf{0}", ImageToUpload);

#if WINDOWS_UWP && ENABLE_DOTNET
		storageFolder = ApplicationData.Current.TemporaryFolder;
		sf = await storageFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
		path = sf.Path;
		await blockBlob.DownloadToFileAsync(sf);
#else
            path = Path.Combine(Application.temporaryCachePath, fileName);
            await blockBlob.DownloadToFileAsync(path, FileMode.Create);
#endif

            WriteLine("File written to " + path);

            // Clean up after the demo 
            WriteLine("5. Delete block Blob");
            await blockBlob.DeleteAsync();

            // When you delete a container it could take several seconds before you can recreate a container with the same
            // name - hence to enable you to run the demo in quick succession the container is not deleted. If you want 
            // to delete the container uncomment the line of code below. 
            WriteLine("6. Delete Container -- Note that it will take a few seconds before you can recreate a container with the same name");
            await container.DeleteAsync();

            WriteLine("-- Test Complete --");
        }

        private async Task BasicStoragePageBlobOperationsAsync()
        {
            WriteLine("-- Testing PageBlob --");

            const string PageBlobName = "samplepageblob";

            // Create a blob client for interacting with the blob service.
            CloudBlobClient blobClient = StorageAccount.CreateCloudBlobClient();

            // Create a container for organizing blobs within the storage account.
            WriteLine("1. Creating Container");
            CloudBlobContainer container = blobClient.GetContainerReference("democontainerpageblob");
            await container.CreateIfNotExistsAsync();

            // Create a page blob in the newly created container.  
            WriteLine("2. Creating Page Blob");
            CloudPageBlob pageBlob = container.GetPageBlobReference(PageBlobName);
            await pageBlob.CreateAsync(512 * 2 /*size*/); // size needs to be multiple of 512 bytes

            // Write to a page blob 
            WriteLine("2. Write to a Page Blob");
            byte[] samplePagedata = new byte[512];
            Random random = new Random();
            random.NextBytes(samplePagedata);
            await pageBlob.UploadFromByteArrayAsync(samplePagedata, 0, samplePagedata.Length);

            // List all blobs in this container. Because a container can contain a large number of blobs the results 
            // are returned in segments (pages) with a maximum of 5000 blobs per segment. You can define a smaller size
            // using the maxResults parameter on ListBlobsSegmentedAsync.
            WriteLine("3. List Blobs in Container");
            BlobContinuationToken token = null;
            do
            {
                BlobResultSegment resultSegment = await container.ListBlobsSegmentedAsync(token);
                token = resultSegment.ContinuationToken;
                foreach (IListBlobItem blob in resultSegment.Results)
                {
                    // Blob type will be CloudBlockBlob, CloudPageBlob or CloudBlobDirectory
                    WriteLine(string.Format("{0} (type: {1}", blob.Uri, blob.GetType()));
                }
            } while (token != null);

            // Read from a page blob
            WriteLine("4. Read from a Page Blob");
            int bytesRead = await pageBlob.DownloadRangeToByteArrayAsync(samplePagedata, 0, 0, samplePagedata.Length);
            WriteLine($"Read {bytesRead} bytes from blob");

            // Clean up after the demo 
            WriteLine("5. Delete page Blob");
            await pageBlob.DeleteAsync();

            // When you delete a container it could take several seconds before you can recreate a container with the same
            // name - hence to enable you to run the demo in quick succession the container is not deleted. If you want 
            // to delete the container uncomment the line of code below. 
            WriteLine("6. Delete Container");
            await container.DeleteAsync();

            WriteLine("-- Test Complete --");
        }
    }

}