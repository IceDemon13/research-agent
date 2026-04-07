using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class UpdateOrderWarrantyProductDto
    {
        public UpdateOrderWarrantyProductDto(int orderProductId, int warrantyId)
        {
            OrderProductId = orderProductId;
            WarrantyId = warrantyId;
        }

        [JsonProperty("order_product_id")]
        public int OrderProductId { get; set; }

        [JsonProperty("warranty_id")]
        public int WarrantyId { get; set; }
    }
}