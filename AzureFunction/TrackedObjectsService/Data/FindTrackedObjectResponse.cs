using Microsoft.Azure.Cosmos.Table;

namespace TrackedObjectsService.Data
{
    public class FindTrackedObjectResponse
    {
        public bool IsFound { get; set; }
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool HasSpatialAnchor { get; set; }
        public bool HasCustomVision { get; set; }

        public static FindTrackedObjectResponse CreateFromDynamicTableEntity(DynamicTableEntity entity)
        {
            var instance = new FindTrackedObjectResponse();

            if (entity == null)
            {
                instance.IsFound = false;
                return instance;
            }

            instance.IsFound = true;
            instance.Id = entity.RowKey;
            instance.Name = entity.Properties[nameof(instance.Name)].ToString();
            if (entity.Properties.ContainsKey(nameof(instance.Description)))
            {
                instance.Description = entity.Properties[nameof(instance.Description)].StringValue;
            }
            if (entity.Properties.ContainsKey(nameof(instance.HasSpatialAnchor)))
            {
                var booleanValue = entity.Properties[nameof(instance.HasSpatialAnchor)].BooleanValue;
                if (booleanValue != null)
                    instance.HasSpatialAnchor = booleanValue.Value;
            }
            if (entity.Properties.ContainsKey(nameof(instance.HasCustomVision)))
            {
                var booleanValue = entity.Properties[nameof(instance.HasCustomVision)].BooleanValue;
                if (booleanValue != null)
                    instance.HasSpatialAnchor = booleanValue.Value;
            }

            return instance;
        }
    }
}
