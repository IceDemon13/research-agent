using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class OrderIdentityDto
    {
        public OrderIdentityDto(int orderId, IReadOnlyCollection<int> orderProductIds, IReadOnlyCollection<int> externalPaymentIds)
        {
            Id = orderId;
            OrderProductIds = orderProductIds;
            ExternalPaymentIds = externalPaymentIds;
        }

        [JsonProperty("id")]
        public int Id { get; init; }

        [JsonProperty("products")]
        public IReadOnlyCollection<int> OrderProductIds { get; init; }

        [JsonProperty("external_payment_ids")]
        public IReadOnlyCollection<int> ExternalPaymentIds { get; init; }
    }
}