using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class InvoiceDelayDto
    {
        public InvoiceDelayDto(int id, IReadOnlyCollection<InvoiceDelayProductDto> products, DateTime arriveDate, int expireReasonId, int? warehouseId)
        {
            Id = id;
            Products = products;
            ArriveDate = arriveDate;
            ExpireReasonId = expireReasonId;
            WarehouseId = warehouseId;
        }

        [JsonProperty("id")]
        public int Id { get; private set; }

        [JsonProperty("products")]
        public IReadOnlyCollection<InvoiceDelayProductDto> Products { get; private set; }

        [JsonProperty("arrive_date")]
        public DateTime ArriveDate { get; private set; }

        [JsonProperty("expire_reason_id")]
        public int ExpireReasonId { get; private set; }

        [JsonProperty("warehouse_id")]
        public int? WarehouseId { get; private set; }
    }
}