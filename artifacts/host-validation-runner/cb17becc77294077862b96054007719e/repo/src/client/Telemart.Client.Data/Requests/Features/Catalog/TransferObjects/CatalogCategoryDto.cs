using Newtonsoft.Json;

namespace Telemart.Client.Data.Requests.Features.Catalog.TransferObjects
{
    public class CatalogCategoryDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("parent_id")]
        public int ParentId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("level_depth")]
        public int Level { get; set; }

        [JsonProperty("parent_level_depth")]
        public int ParentLevel { get; set; }

        [JsonProperty("is_parent")]
        public int IsParent { get; set; }

        [JsonProperty("active")]
        public double Active { get; set; }

        [JsonProperty("count")]
        public int FoundQuantity { get; set; }
    }
}