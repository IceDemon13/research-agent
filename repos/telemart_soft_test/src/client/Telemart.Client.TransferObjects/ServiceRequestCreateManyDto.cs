using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class ServiceRequestCreateManyDto
    {
        [JsonProperty("order_id")]
        public int OrderId { get; set; }

        [JsonProperty("product")]
        public List<ServiceRequestCreateManyProductDto> Products { get; set; }

        [JsonProperty("requirement_id")]
        public int Requirement { get; set; }

        [JsonProperty("service_repair_type_id")]
        public int? ServiceRepairTypeId { get; set; }

        [JsonProperty("requirement_payment_id")]
        public int? RequirementPaymentId { get; set; }

        [JsonProperty("requirement_cashbox_id")]
        public int? RequirementCashboxId { get; set; }

        #region Client

        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("fio")]
        public string Fio { get; set; }

        [JsonProperty("phone")]
        public string Phone { get; set; }

        [JsonProperty("phone2")]
        public string Phone2 { get; set; }

        [JsonProperty("requisites")]
        public RefundRequisitesDto Requisites { get; set; }

        #endregion

        #region Logistics

        [JsonProperty("city_id")]
        public int? CityId { get; set; }

        [JsonProperty("delivery_data_out")]
        public DeliveryDataDto DeliveryDataOut { get; set; }

        [JsonProperty("carry_in_id")]
        public int CarryInId { get; set; }

        [JsonProperty("carry_out_id")]
        public int CarryOutId { get; set; }

        [JsonProperty("warehouse_in_id")]
        public int WarehouseInId { get; set; }

        [JsonProperty("send_to")]
        public string SendTo { get; set; }

        #endregion
    }
}