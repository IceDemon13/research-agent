using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Warehouse.Delivery
{
    public sealed record DeleteManyDeliveriesDto
    {
        [JsonProperty("delivery_ids")]
        public IReadOnlyCollection<int> DeliveryIds { get; init; }
    }
}