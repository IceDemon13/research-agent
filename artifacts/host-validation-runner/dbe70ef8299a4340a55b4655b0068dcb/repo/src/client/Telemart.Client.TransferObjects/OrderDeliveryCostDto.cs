using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class OrderDeliveryCostDto
    {
        public OrderDeliveryCostDto(
            int subdivisionId,
            int carryId,
            int appliedBonusesQuantity,
            int? paymentId,
            IEnumerable<OrderProductDeliveryCostDto> orderProducts)
        {
            SubdivisionId = subdivisionId;
            CarryId = carryId;
            PaymentId = paymentId;
            AppliedBonusesQuantity = appliedBonusesQuantity;
            OrderProducts = orderProducts.ToArray();
        }

        [JsonProperty("carry_id")]
        public int CarryId { get; set; }

        [JsonProperty("payment_id")]
        public int? PaymentId { get; set; }

        [JsonProperty("applied_bonuses_quantity")]
        public int AppliedBonusesQuantity { get; set; }

        [JsonProperty("subdivision_id")]
        public int SubdivisionId { get; set; }

        [JsonProperty("products")]
        public IReadOnlyCollection<OrderProductDeliveryCostDto> OrderProducts { get; set; }
    }
}