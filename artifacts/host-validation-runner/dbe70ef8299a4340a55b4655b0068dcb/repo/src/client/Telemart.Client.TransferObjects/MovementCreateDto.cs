using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class MovementCreateDto
    {
        public MovementCreateDto(
            int warehouseFromId,
            int warehouseToId,
            DateTime dateOut,
            DateTime dateDeparture,
            DateTime dateArrive,
            DateTime dateIn,
            bool moveFreeStocks,
            bool createEmpty,
            int? carryId,
            int? deliveryTypeId,
            IReadOnlyCollection<int> purposeIds,
            bool checkExists,
            bool system,
            bool ignoreDateOutValidation)
        {
            WarehouseFromId = warehouseFromId;
            WarehouseToId = warehouseToId;
            DateOut = dateOut;
            DateIn = dateIn;
            DateArrive = dateArrive;
            DateDeparture = dateDeparture;
            MoveFreeStocks = moveFreeStocks;
            CreateEmpty = createEmpty;
            CarryId = carryId;
            DeliveryTypeId = deliveryTypeId;
            PurposeIds = purposeIds;
            CheckExists = checkExists;
            System = system;
            IgnoreDateOutValidation = ignoreDateOutValidation;
        }

        [JsonProperty("warehouse_from_id")]
        public int WarehouseFromId { get; init; }

        [JsonProperty("warehouse_to_id")]
        public int WarehouseToId { get; init; }

        [JsonProperty("date_out")]
        public DateTime DateOut { get; init; }

        [JsonProperty("date_departure")]
        public DateTime DateDeparture { get; init; }

        [JsonProperty("date_arrive")]
        public DateTime DateArrive { get; init; }

        [JsonProperty("date_in")]
        public DateTime DateIn { get; init; }

        [JsonProperty("move_free_stocks")]
        public bool MoveFreeStocks { get; init; }

        [JsonProperty("create_empty")]
        public bool CreateEmpty { get; init; }

        [JsonProperty("carry_id")]
        public int? CarryId { get; init; }

        [JsonProperty("delivery_type_id")]
        public int? DeliveryTypeId { get; init; }

        [JsonProperty("purpose_ids")]
        public IReadOnlyCollection<int> PurposeIds { get; init; }

        [JsonProperty("check_exists")]
        public bool CheckExists { get; init; }

        [JsonProperty("system")]
        public bool System { get; init; }

        [JsonProperty("ignore_date_out_validation")]
        public bool IgnoreDateOutValidation { get; init; }
    }
}