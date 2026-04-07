using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class MovementDelayDto
    {
        public MovementDelayDto(int id, IReadOnlyCollection<MovementDelayProductDto> products, DateTime? sendDate, DateTime? arriveDate, DateTime? receiveDate, int expireReasonId, int? warehouseId)
        {
            Id = id;
            Products = products;
            SendDate = sendDate;
            ArriveDate = arriveDate;
            ReceiveDate = receiveDate;
            ExpireReasonId = expireReasonId;
            WarehouseId = warehouseId;
        }

        [JsonProperty("id")]
        public int Id { get; private set; }

        [JsonProperty("products")]
        public IReadOnlyCollection<MovementDelayProductDto> Products { get; private set; }

        public DateTime? SendDate { get; private set; }

        [JsonProperty("arrive_date")]
        public DateTime? ArriveDate { get; private set; }

        [JsonProperty("receive_date")]
        public DateTime? ReceiveDate { get; private set; }

        [JsonProperty("expire_reason_id")]
        public int ExpireReasonId { get; private set; }

        [JsonProperty("warehouse_id")]
        public int? WarehouseId { get; private set; }
    }
}