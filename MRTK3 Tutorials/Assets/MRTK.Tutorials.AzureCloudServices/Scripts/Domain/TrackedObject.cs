// Copyright (c) Microsoft Corporation. 
// Licensed under the MIT License.

using Microsoft.WindowsAzure.Storage.Table;

namespace MRTK.Tutorials.AzureCloudServices.Scripts.Domain
{
    public class TrackedObject : TableEntity
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string ThumbnailBlobName { get; set; }
        public string SpatialAnchorId { get; set; }
        public string CustomVisionTagId { get; set; } // for simplicity there is one tag per project
        public string CustomVisionTagName { get; set; }
        public bool HasBeenTrained { get; set; }

        public TrackedObject() { }

        public TrackedObject(string name)
        {
            Name = name;
            RowKey = name;
        }
    }
}
