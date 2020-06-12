using System.Collections.Generic;

namespace MRTK.Tutorials.AzureCloudPower.Domain
{
    public class CustomVision
    {
        public string ProjectId { get; set; }
        public string IterationId { get; set; }
        public string PublishModelName { get; set; }
        public string TagName { get; set; }
        public string TagId { get; set; } // for simplicity there is one tag per project
        public List<string> ImageIds { get; set; } = new List<string>();
    }
}