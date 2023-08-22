// Copyright (c) Microsoft Corporation. 
// Licensed under the MIT License.

using Microsoft.WindowsAzure.Storage.Table;

namespace MRTK.Tutorials.AzureCloudServices.Scripts.Domain
{
    public class Project : TableEntity
    {
        public string Name { get; set; }
        public string CustomVisionIterationId { get; set; }
        public string CustomVisionPublishedModelName { get; set; }
    }
}
