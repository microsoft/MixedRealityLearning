// Copyright (c) Microsoft Corporation. 
// Licensed under the MIT License.

using System;
using Newtonsoft.Json;

namespace MRTK.Tutorials.AzureCloudServices.Scripts.Dtos
{
    public class TrainProjectResult
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("created")]
        public DateTimeOffset Created { get; set; }

        [JsonProperty("lastModified")]
        public DateTimeOffset LastModified { get; set; }

        [JsonProperty("projectId")]
        public string ProjectId { get; set; }

        [JsonProperty("exportable")]
        public bool Exportable { get; set; }

        [JsonProperty("domainId")]
        public object DomainId { get; set; }

        [JsonProperty("exportableTo")]
        public string[] ExportableTo { get; set; }

        [JsonProperty("trainingType")]
        public string TrainingType { get; set; }

        [JsonProperty("reservedBudgetInHours")]
        public long ReservedBudgetInHours { get; set; }

        [JsonProperty("publishName")]
        public string PublishName { get; set; }
        

        public bool IsCompleted()
        {
            return Status == "Completed";
        }
    }
}