using System;
using Newtonsoft.Json;

namespace MRTK.Tutorials.AzureCloudPower.Dtos
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