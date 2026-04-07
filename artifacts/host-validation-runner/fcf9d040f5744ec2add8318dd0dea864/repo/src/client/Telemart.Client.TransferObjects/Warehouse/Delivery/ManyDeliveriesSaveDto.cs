using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Warehouse.Delivery
{
    public sealed record ManyDeliveriesSaveDto
    {
        [JsonProperty("warehouse_id")]
        public int WarehouseId { get; init; }

        [JsonProperty("deliveries")]
        public DeliverySaveDto[] Deliveries { get; init; }
    }
}