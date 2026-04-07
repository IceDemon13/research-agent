using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Segment
{
    public sealed class SegmentUpdateDto
    {
        public SegmentUpdateDto(
            int id,
            string name,
            int employeeId,
            bool autoShowcase,
            IReadOnlyCollection<SegmentCategoryFeatureSimpleDto> categoryFeatures)
        {
            Name = name;
            EmployeeId = employeeId;
            CategoryFeatures = categoryFeatures;
            Id = id;
            AutoShowcase = autoShowcase;
        }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("employee_id")]
        public int EmployeeId { get; set; }

        [JsonProperty("auto_showcase")]
        public bool AutoShowcase { get; set; }

        [JsonProperty("category_features")]
        public IReadOnlyCollection<SegmentCategoryFeatureSimpleDto> CategoryFeatures { get; set; }
    }
}