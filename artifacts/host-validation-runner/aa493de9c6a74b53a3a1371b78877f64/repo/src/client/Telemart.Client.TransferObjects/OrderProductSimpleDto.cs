using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class OrderProductSimpleDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("warehouse_id")]
        public int? WarehouseId { get; init; }

        [JsonProperty("price_out")]
        public decimal PriceOut { get; set; }

        [JsonProperty("quantity")]
        public int Quantity { get; init; }

        [JsonProperty("currency_out_id")]
        public int CurrencyOutId { get; init; }

        [JsonProperty("name_full_ru")]
        public string NameFullRu { get; init; }

        [JsonProperty("name_full_ukr")]
        public string NameFullUa { get; init; }

        [JsonProperty("name_full_en")]
        public string NameFullEn { get; init; }

        [JsonProperty("is_gift")]
        public bool IsGift { get; init; }

        [JsonProperty("state_id")]
        public int StateId { get; init; }

        [JsonProperty("source_id")]
        public int SourceId { get; init; }

        [JsonProperty("source_text")]
        public string SourceText { get; set; }

        [JsonProperty("source_date")]
        public DateTime? SourceDate { get; set; }

        [JsonProperty("is_additional_service")]
        public bool IsAdditionalService { get; set; }

        [JsonProperty("is_additional_service_consumable")]
        public bool IsAdditionalServiceConsumable { get; init; }

        [JsonProperty("product_type_id")]
        public int ProductTypeId { get; init; }

        [JsonProperty("parent_record_id")]
        public int? ParentRecordId { get; set; }

        [JsonProperty("created_by")]
        public int? CreatedBy { get; set; }

        [JsonProperty("created_on")]
        public DateTime? CreatedOn { get; set; }
    }
}