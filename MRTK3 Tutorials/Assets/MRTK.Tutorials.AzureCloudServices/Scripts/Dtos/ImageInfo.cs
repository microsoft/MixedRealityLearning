// Copyright (c) Microsoft Corporation. 
// Licensed under the MIT License.

using System;
using Newtonsoft.Json;

namespace MRTK.Tutorials.AzureCloudServices.Scripts.Dtos
{
    public class ImageInfo
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("created")]
        public DateTimeOffset Created { get; set; }

        [JsonProperty("width")]
        public int Width { get; set; }

        [JsonProperty("height")]
        public int Height { get; set; }

        [JsonProperty("resizedImageUri")]
        public string ResizedImageUri { get; set; }

        [JsonProperty("originalImageUri")]
        public string OriginalImageUri { get; set; }

        [JsonProperty("thumbnailUri")]
        public string ThumbnailUri { get; set; }

        [JsonProperty("tags")]
        public Tag[] Tags { get; set; }
    }
}