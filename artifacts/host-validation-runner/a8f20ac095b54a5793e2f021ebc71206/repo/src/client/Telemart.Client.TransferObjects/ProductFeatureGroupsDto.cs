using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class ProductFeatureGroupsDto
    {
        [JsonProperty("product_id")]
        public int ProductId { get; set; }

        [JsonProperty("groups")]
        public List<ProductFeatureGroupDto> AttributeGroups { get; set; }
    }
}