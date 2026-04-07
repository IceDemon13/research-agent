using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class NoProductCreateDto
    {
        public NoProductCreateDto(
            int? noProductReasonId,
            int? orderId,
            int? productId)
        {
            OrderId = orderId;
            NoProductReasonId = noProductReasonId;
            ProductId = productId;
        }

        [JsonProperty("no_product_reason_id")]
        public int? NoProductReasonId { get; set; }

        [JsonProperty("order_id")]
        public int? OrderId { get; set; }

        [JsonProperty("product_id")]
        public int? ProductId { get; set; }

        [JsonProperty("supplier_id")]
        public int? SupplierId { get; set; }

        [JsonProperty("supplier_name")]
        public string SupplierName { get; set; }

        [JsonProperty("incorrect_price")]
        public decimal? IncorrectPrice { get; set; }

        [JsonProperty("correct_price")]
        public decimal? CorrectPrice { get; set; }

        [JsonProperty("comment")]
        public string Comment { get; set; }

        public void SupplierInfo(int supplierId, string supplierName)
        {
            SupplierId = supplierId;
            SupplierName = supplierName;
        }

        public void SetPriceInfo(decimal correct, decimal incorrect)
        {
            CorrectPrice = correct;
            IncorrectPrice = incorrect;
        }
    }
}