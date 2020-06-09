using System.Collections.Generic;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;

namespace MRTK.Tutorials.AzureCloudPower.Domain
{
    public class ObjectProject : TableEntity
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string ThumbnailBlobUrl { get; set; }
        public string SpatialAnchorId { get; set; }
        [IgnoreProperty]
        public CustomVision CustomVision { get; set; } = new CustomVision();

        public override IDictionary<string, EntityProperty> WriteEntity(OperationContext operationContext)
        {
            var props = base.WriteEntity(operationContext);
            var visionProjectJson = JsonConvert.SerializeObject(CustomVision);
            props.Add(nameof(CustomVision), EntityProperty.GeneratePropertyForString(visionProjectJson));

            return props;
        }

        public override void ReadEntity(IDictionary<string, EntityProperty> properties, OperationContext operationContext)
        {
            base.ReadEntity(properties, operationContext);
            var visionProjectJson = properties[nameof(CustomVision)].StringValue;
            CustomVision = JsonConvert.DeserializeObject<CustomVision>(visionProjectJson);
        }
    }
}
