// Copyright (c) Microsoft Corporation. 
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace MRTK.Tutorials.AzureCloudServices.Scripts.Dtos
{
    public class ImagesCreatedResult
    {
        [JsonProperty("isBatchSuccessful")]
        public bool IsBatchSuccessful { get; set; }

        [JsonProperty("images")]
        public ImageElement[] Images { get; set; }
    }
}