using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Debezium
{
    public sealed class ShowcaseHistoryDto
    {
        [JsonProperty("showcase_history_id")]
        public long ShowcaseHistoryId { get; set; }

        [JsonProperty("created_on")]
        public DateTime CreatedOn { get; set; }

        [JsonProperty("showcase_id")]
        public int ShowcaseId { get; set; }

        [JsonProperty("id_product")]
        public int ProductId { get; set; }

        [JsonProperty("product_full_name_ru")]
        public string ProductFullNameRu { get; set; }

        [JsonProperty("product_full_name_ukr")]
        public string ProductFullNameUkr { get; set; }

        [JsonProperty("product_full_name_en")]
        public string ProductFullNameEn { get; set; }

        [JsonProperty("id_warehouse")]
        public int WarehouseId { get; set; }

        [JsonProperty("category_id")]
        public int CategoryId { get; set; }

        [JsonProperty("capacity_new")]
        public int CapacityNew { get; set; }

        [JsonProperty("capacity_old")]
        public int CapacityOld { get; set; }

        [JsonProperty("active_old")]
        public bool ActiveOld { get; set; }

        [JsonProperty("active_new")]
        public bool ActiveNew { get; set; }

        [JsonProperty("modified_on")]
        public DateTime ModifiedOn { get; set; }

        [JsonProperty("modified_by")]
        public int ModifiedBy { get; set; }
    }
}