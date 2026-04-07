using Newtonsoft.Json;

namespace Telemart.Client.Data.Requests.Features.Catalog.TransferObjects.CheckCompatibility
{
    public class ProductQuantityDto
    {
        public ProductQuantityDto(int productId, int quantity)
        {
            ProductId = productId;
            Quantity = quantity;
        }

        [JsonProperty("product_id")]
        public int ProductId { get; set; }

        [JsonProperty("quantity")]
        public int Quantity { get; set; }
    }
}