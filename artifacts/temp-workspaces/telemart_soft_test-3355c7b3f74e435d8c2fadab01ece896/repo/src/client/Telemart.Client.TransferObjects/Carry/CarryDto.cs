using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Carry
{
    public class CarryDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("type_id")]
        public int CarryTypeId { get; set; }

        [JsonProperty("insurance_percent")]
        public int InsurancePercent { get; set; }

        [JsonProperty("contractor_id")]
        public int ContractorId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("name_short")]
        public string NameShort { get; set; }

        [JsonProperty("name_ua")]
        public string NameUa { get; set; }

        [JsonProperty("name_en")]
        public string NameEn { get; set; }

        [JsonProperty("position")]
        public int Position { get; set; }

        [JsonProperty("comission")]
        public decimal Comission { get; set; }

        [JsonProperty("order_cost_limit")]
        public int? OrderCostLimit { get; set; }

        [JsonProperty("active")]
        public bool Active { get; set; }

        [JsonProperty("use_in_movements")]
        public bool UseInMovements { get; set; }

        [JsonProperty("use_in_order")]
        public bool UseInOrder { get; set; }

        [JsonProperty("is_local")]
        public bool IsLocal { get; set; }

        [JsonProperty("require_last_name")]
        public bool RequireLastName { get; set; }

        [JsonProperty("require_middle_name")]
        public bool RequireMiddleName { get; set; }

        [JsonProperty("our_warehouse_shipment")]
        public bool OurWarehouseShipment { get; set; }

        [JsonProperty("max_left_to_pay_usd")]
        public decimal MaxLeftToPayUsd { get; set; }

        [JsonProperty("max_left_to_pay_uah")]
        public decimal MaxLeftToPayUah { get; set; }

        [JsonProperty("delivery_cost")]
        public int DeliveryCost { get; set; }

        [JsonProperty("min_free_delivery_cost")]
        public int MinFreeDeliveryCost { get; set; }

        [JsonProperty("ttn_regex")]
        public string TtnRegex { get; set; }

        [JsonProperty("weight_limit")]
        public int? WeightLimit { get; set; }

        [JsonProperty("use_in_service_movement")]
        public bool UseInServiceMovement { get; set; }

        [JsonProperty("can_switch_in_orders")]
        public bool CanSwitchInOrders { get; set; }

        [JsonProperty("sticker_required")]
        public bool StickerRequired { get; set; }

        [JsonProperty("allow_free_under_limit")]
        public bool AllowFreeUnderLimit { get; set; }

        [JsonProperty("carry_provider_id")]
        public int? CarryProviderId { get; set; }

        [JsonProperty("volume_weight_coef")]
        public int? VolumeWeightCoef { get; init; }

        [JsonProperty("schedule_delivery")]
        public bool ScheduleDelivery { get; init; }

        [JsonProperty("drivers")]
        public int[] Drivers { get; init; }
    }
}