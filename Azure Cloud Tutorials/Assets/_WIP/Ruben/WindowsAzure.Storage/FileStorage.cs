using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.WindowsAzure.Storage.File;
using UnityEngine;
#if WINDOWS_UWP && ENABLE_DOTNET
using Windows.Storage;
#endif

namespace Microsoft.WindowsAzure.Storage
{

    public class FileStorage : BaseStorage
    {
        public string DemoShare = "demofileshare";
        public string DemoDirectory = "demofiledirectory";
        public string ImageToUpload = "HelloWorld.png";

        public async void FileStorageTest()
        {
            ClearOutput();
            WriteLine("-- Testing File Storage --");

            WriteLine("0. Creating file client");

            // Create a file client for interacting with the file service.
            CloudFileClient fileClient = StorageAccount.CreateCloudFileClient();

            // Create a share for organizing files and directories within the storage account.
            WriteLine("1. Creating file share");
            CloudFileShare share = fileClient.GetShareReference(DemoShare);

            try
            {
                await share.CreateIfNotExistsAsync();
            }
            catch (StorageException)
            {
                WriteLine("Please make sure your storage account has storage file endpoint enabled and specified correctly in the app.config - then restart the sample.");
                throw;
            }

            // Get a reference to the root directory of the share.        
            CloudFileDirectory root = share.GetRootDirectoryReference();

            // Create a directory under the root directory 
            WriteLine("2. Creating a directory under the root directory");
            CloudFileDirectory dir = root.GetDirectoryReference(DemoDirectory);
            await dir.CreateIfNotExistsAsync();

            // Uploading a local file to the directory created above 
            WriteLine("3. Uploading a file to directory");
            CloudFile file = dir.GetFileReference(ImageToUpload);

#if WINDOWS_UWP && ENABLE_DOTNET
		StorageFolder storageFolder = await StorageFolder.GetFolderFromPathAsync(Application.streamingAssetsPath.Replace('/', '\\'));
		StorageFile sf = await storageFolder.GetFileAsync(ImageToUpload);
		await file.UploadFromFileAsync(sf);
#else
            await file.UploadFromFileAsync(Path.Combine(Application.streamingAssetsPath, ImageToUpload));
#endif

            // List all files/directories under the root directory
            WriteLine("4. List Files/Directories in root directory");
            List<IListFileItem> results = new List<IListFileItem>();
            FileContinuationToken token = null;
            do
            {
                FileResultSegment resultSegment = await share.GetRootDirectoryReference().ListFilesAndDirectoriesSegmentedAsync(token);
                results.AddRange(resultSegment.Results);
                token = resultSegment.ContinuationToken;
            }
            while (token != null);

            // Print all files/directories listed above
            foreach (IListFileItem listItem in results)
            {
                // listItem type will be CloudFile or CloudFileDirectory
                WriteLine(string.Format("- {0} (type: {1})", listItem.Uri, listItem.GetType()));
            }

            // Download the uploaded file to your file system
            string path;
            WriteLine(string.Format("5. Download file from {0}", file.Uri.AbsoluteUri));
            string fileName = string.Format("CopyOf{0}", ImageToUpload);

#if WINDOWS_UWP && ENABLE_DOTNET
		storageFolder = ApplicationData.Current.TemporaryFolder;
		sf = await storageFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
		path = sf.Path;
		await file.DownloadToFileAsync(sf);
#else
            path = Path.Combine(Application.temporaryCachePath, fileName);
            await file.DownloadToFileAsync(path, FileMode.Create);
#endif

            WriteLine("File written to " + path);

            // Clean up after the demo 
            WriteLine("6. Delete file");
            await file.DeleteAsync();

            // When you delete a share it could take several seconds before you can recreate a share with the same
            // name - hence to enable you to run the demo in quick succession the share is not deleted. If you want 
            // to delete the share uncomment the line of code below. 
            WriteLine("7. Delete Share -- Note that it will take a few seconds before you can recreate a share with the same name");
            await share.DeleteAsync();

            WriteLine("-- Test Complete --");
        }
    }

}