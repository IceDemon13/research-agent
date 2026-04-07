using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Catalog
{
    public class ProductMovingDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("category_from_id")]
        public int CategoryFromId { get; set; }

        [JsonProperty("category_to_id")]
        public int CategoryToId { get; set; }

        [JsonProperty("properties")]
        public IReadOnlyCollection<ProductMovingPropertyDto> Properties { get; set; }

        [JsonProperty("validations")]
        public IReadOnlyCollection<ValidationResultItemDto> Validations { get; set; }
    }
}