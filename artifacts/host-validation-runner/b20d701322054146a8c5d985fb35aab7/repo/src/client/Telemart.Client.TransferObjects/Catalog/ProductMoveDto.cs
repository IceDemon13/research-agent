using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Catalog
{
    public class ProductMoveDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("category_id")]
        public int CategoryId { get; set; }

        [JsonProperty("properties")]
        public IReadOnlyCollection<ProductMovePropertyDto> Properties { get; set; }
    }
}