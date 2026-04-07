using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Catalog
{
    public class ProductMovingRequest
    {
        public ProductMovingRequest(int id, int categoryToId)
        {
            Id = id;
            CategoryToId = categoryToId;
        }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("category_to_id")]
        public int CategoryToId { get; set; }
    }
}