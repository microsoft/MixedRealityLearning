// Copyright (c) Microsoft Corporation. 
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace MRTK.Tutorials.AzureCloudServices.Scripts.Dtos
{
    public class ImageElement
    {
        [JsonProperty("sourceUrl")]
        public string SourceUrl { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("image")]
        public ImageInfo Image { get; set; }
    }
}