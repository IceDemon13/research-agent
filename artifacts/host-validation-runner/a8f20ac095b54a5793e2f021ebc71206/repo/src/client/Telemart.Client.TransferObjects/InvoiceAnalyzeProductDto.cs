using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed record InvoiceAnalyzeProductDto
    {
        public InvoiceAnalyzeProductDto(int productId, int quantity, int orderQuantity, decimal price)
        {
            ProductId = productId;
            Quantity = quantity;
            OrderQuantity = orderQuantity;
        }

        [JsonProperty("product_id")]
        public int ProductId { get; init; }

        [JsonProperty("quantity")]
        public int Quantity { get; init; }

        [JsonProperty("order_quantity")]
        public int OrderQuantity { get; init; }

        [JsonProperty("price")]
        public decimal Price { get; init; }
    }
}