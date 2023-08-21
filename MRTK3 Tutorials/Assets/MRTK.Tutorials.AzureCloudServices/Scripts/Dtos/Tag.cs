// Copyright (c) Microsoft Corporation. 
// Licensed under the MIT License.

using System;
using Newtonsoft.Json;

namespace MRTK.Tutorials.AzureCloudServices.Scripts.Dtos
{
    public class Tag
    {
        [JsonProperty("tagId")]
        public string TagId { get; set; }

        [JsonProperty("created")]
        public DateTimeOffset Created { get; set; }

        [JsonProperty("tagName")]
        public string TagName { get; set; }
    }
}