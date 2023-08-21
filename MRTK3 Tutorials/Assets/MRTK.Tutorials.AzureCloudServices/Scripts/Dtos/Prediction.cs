// Copyright (c) Microsoft Corporation. 
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace MRTK.Tutorials.AzureCloudServices.Scripts.Dtos
{
    public class Prediction
    {
        [JsonProperty("probability")]
        public double Probability { get; set; }

        [JsonProperty("tagId")]
        public string TagId { get; set; }

        [JsonProperty("tagName")]
        public string TagName { get; set; }
    }
}