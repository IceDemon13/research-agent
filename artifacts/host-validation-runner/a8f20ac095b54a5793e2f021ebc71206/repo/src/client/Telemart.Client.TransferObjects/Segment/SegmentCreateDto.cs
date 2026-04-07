using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Segment
{
    public class SegmentCreateDto
    {
        public SegmentCreateDto(
            string name,
            int categoryId,
            int employeeId,
            bool autoShowcase,
            IReadOnlyCollection<SegmentCategoryFeatureSimpleDto> categoryFeatures)
        {
            Name = name;
            CategoryId = categoryId;
            EmployeeId = employeeId;
            AutoShowcase = autoShowcase;
            CategoryFeatures = categoryFeatures;
        }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("category_id")]
        public int CategoryId { get; set; }

        [JsonProperty("employee_id")]
        public int EmployeeId { get; set; }

        [JsonProperty("auto_showcase")]
        public bool AutoShowcase { get; set; }

        [JsonProperty("category_features")]
        public IReadOnlyCollection<SegmentCategoryFeatureSimpleDto> CategoryFeatures { get; set; }
    }
}