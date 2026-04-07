using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.RobotProperty
{
    public sealed class SaveRobotCategoryPropertyValuesDto
    {
        public SaveRobotCategoryPropertyValuesDto(
            int categoryId,
            int? productTypeId,
            IReadOnlyCollection<RobotCategoryPropertyValueSaveDto> values)
        {
            CategoryId = categoryId;
            ProductTypeId = productTypeId;
            Values = values;
        }

        [JsonProperty("category_id")]
        public int CategoryId { get; init; }

        [JsonProperty("product_type_id")]
        public int? ProductTypeId { get; init; }

        [JsonProperty("values")]
        public IReadOnlyCollection<RobotCategoryPropertyValueSaveDto> Values { get; init; }
    }
}