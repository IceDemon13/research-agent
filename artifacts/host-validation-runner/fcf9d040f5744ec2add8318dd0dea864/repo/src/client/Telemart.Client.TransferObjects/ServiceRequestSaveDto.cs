using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class ServiceRequestSaveDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("fio")]
        public string Fio { get; set; }

        [JsonProperty("phone")]
        public string Phone { get; set; }

        [JsonProperty("phone2")]
        public string Phone2 { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("stated_defect")]
        public string StatedDefect { get; set; }

        [JsonProperty("apppearance")]
        public string Appearance { get; set; }

        [JsonProperty("completeness_comment")]
        public string CompletenessComment { get; set; }

        [JsonProperty("exchange_fund")]
        public string ExchangeFund { get; set; }

        [JsonProperty("comment")]
        public string Comment { get; set; }

        [JsonProperty("warehouse_in_id")]
        public int? WarehouseInId { get; set; }

        [JsonProperty("carry_in_id")]
        public int? CarryInId { get; set; }

        [JsonProperty("carry_out_id")]
        public int? CarryOutId { get; set; }

        [JsonProperty("city_id")]
        public int? CityId { get; set; }

        [JsonProperty("send_to")]
        public string SendTo { get; set; }

        [JsonProperty("delivery_data_out")]
        public DeliveryDataDto DeliveryDataOut { get; set; }

        [JsonProperty("ttn_in")]
        public string TtnIn { get; set; }

        [JsonProperty("documents")]
        public IReadOnlyCollection<ServiceRequestDocumentSimpleSaveDto> Documents { get; set; }

        [JsonProperty("requisites")]
        public RefundRequisitesDto Requisites { get; init; }
    }
}