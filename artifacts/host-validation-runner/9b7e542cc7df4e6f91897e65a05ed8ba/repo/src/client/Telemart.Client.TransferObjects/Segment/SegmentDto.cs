using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Segment
{
    public sealed class SegmentDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("default")]
        public bool Default { get; init; }

        [JsonProperty("category_id")]
        public int CategoryId { get; set; }

        [JsonProperty("category_name")]
        public string CategoryName { get; set; }

        [JsonProperty("employee_id")]
        public int EmployeeId { get; set; }

        [JsonProperty("employee_name")]
        public string EmployeeName { get; set; }

        [JsonProperty("created_on")]
        public DateTime CreatedOn { get; set; }

        [JsonProperty("modified_on")]
        public DateTime ModifiedOn { get; set; }

        [JsonProperty("created_by_name")]
        public string CreatedByName { get; set; }

        [JsonProperty("modified_by_name")]
        public string ModifiedByName { get; set; }

        [JsonProperty("auto_showcase")]
        public bool AutoShowcase { get; set; }

        [JsonProperty("category_features")]
        public IReadOnlyCollection<SegmentCategoryFeatureDto> CategoryFeatures { get; set; }
    }
}