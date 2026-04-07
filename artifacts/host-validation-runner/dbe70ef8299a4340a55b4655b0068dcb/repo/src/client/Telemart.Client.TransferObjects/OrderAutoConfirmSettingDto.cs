using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class OrderAutoConfirmSettingDto
    {
        public OrderAutoConfirmSettingDto(
            int id,
            int carryId,
            int paymentId,
            decimal maxSumLimit,
            double minExtraChargePercent,
            int maxProductQuantity,
            int maxLinesQuantity,
            int[] orderProductSources)
        {
            Id = id;
            CarryId = carryId;
            PaymentId = paymentId;
            MaxSumLimit = maxSumLimit;
            MinExtraChargePercent = minExtraChargePercent;
            MaxProductQuantity = maxProductQuantity;
            MaxLinesQuantity = maxLinesQuantity;
            OrderProductSources = orderProductSources;
        }

        public OrderAutoConfirmSettingDto()
        {
        }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("carry_id")]
        public int CarryId { get; set; }

        [JsonProperty("payment_id")]
        public int PaymentId { get; set; }

        [JsonProperty("max_sum_limit")]
        public decimal MaxSumLimit { get; set; }

        [JsonProperty("min_extra_charge_percent")]
        public double MinExtraChargePercent { get; set; }

        [JsonProperty("max_product_quantity")]
        public int MaxProductQuantity { get; set; }

        [JsonProperty("max_lines_quantity")]
        public int MaxLinesQuantity { get; set; }

        [JsonProperty("order_product_sources")]
        public int[] OrderProductSources { get; set; }
    }
}