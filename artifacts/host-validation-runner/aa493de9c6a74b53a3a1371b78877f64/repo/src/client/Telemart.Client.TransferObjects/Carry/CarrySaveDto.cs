using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Carry
{
    public class CarrySaveDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("contractor_id")]
        public int ContractorId { get; set; }

        [JsonProperty("insurance_percent")]
        public int InsurancePercent { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("name_short")]
        public string NameShort { get; set; }

        [JsonProperty("name_ua")]
        public string NameUa { get; set; }

        [JsonProperty("name_en")]
        public string NameEn { get; set; }

        [JsonProperty("active")]
        public bool Active { get; set; }

        [JsonProperty("is_local")]
        public bool IsLocal { get; set; }

        [JsonProperty("comission")]
        public decimal Comission { get; set; }

        [JsonProperty("order_cost_limit")]
        public int? OrderCostLimit { get; set; }

        [JsonProperty("use_in_movements")]
        public bool UseInMovements { get; set; }

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

        [JsonProperty("drivers")]
        public int[] Drivers { get; init; }
    }
}