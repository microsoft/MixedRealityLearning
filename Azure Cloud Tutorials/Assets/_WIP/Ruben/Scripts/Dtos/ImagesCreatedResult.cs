using Newtonsoft.Json;

namespace MRTK.Tutorials.AzureCloudPower.Dtos
{
    public class ImagesCreatedResult
    {
        [JsonProperty("isBatchSuccessful")]
        public bool IsBatchSuccessful { get; set; }

        [JsonProperty("images")]
        public ImageElement[] Images { get; set; }
    }
}