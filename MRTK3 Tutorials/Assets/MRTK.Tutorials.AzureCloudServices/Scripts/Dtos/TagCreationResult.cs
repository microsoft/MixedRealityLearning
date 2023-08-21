// Copyright (c) Microsoft Corporation. 
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace MRTK.Tutorials.AzureCloudServices.Scripts.Dtos
{
    public class TagCreationResult
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("imageCount")]
        public int ImageCount { get; set; }
    }
}