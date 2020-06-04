using System.Collections.Generic;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;

namespace MRTK.Tutorials.AzureCloudPower.Domain
{
    public class TrackedObject : TableEntity
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string ThumbnailBlobUrl { get; set; }
        public string SpatialAnchorId { get; set; }
        public string VisionTagId { get; set; }
        [IgnoreProperty]
        public List<string> VisionTrainingImagesUrls { get; set; } = new List<string>();

        public override IDictionary<string, EntityProperty> WriteEntity(OperationContext operationContext)
        {
            var props = base.WriteEntity(operationContext);
            var urls = JsonConvert.SerializeObject(VisionTrainingImagesUrls);
            props.Add(nameof(VisionTrainingImagesUrls), EntityProperty.GeneratePropertyForString(urls));

            return props;
        }

        public override void ReadEntity(IDictionary<string, EntityProperty> properties, OperationContext operationContext)
        {
            base.ReadEntity(properties, operationContext);
            var urls = properties[nameof(VisionTrainingImagesUrls)].StringValue;
            VisionTrainingImagesUrls = JsonConvert.DeserializeObject<List<string>>(urls);
        }
    }
}
