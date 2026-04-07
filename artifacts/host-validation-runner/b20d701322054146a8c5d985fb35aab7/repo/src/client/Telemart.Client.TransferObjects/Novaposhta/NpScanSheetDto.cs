using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Novaposhta
{
    public class NpScanSheetDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("ref")]
        public string Ref { get; set; }

        [JsonProperty("np_scansheet_link")]
        public string NpScanSheetLink { get; set; }

        [JsonProperty("np_contractor_name")]
        public string NpContractorName { get; set; }

        [JsonProperty("created_on")]
        public DateTime CreatedOn { get; set; }

        [JsonProperty("created_by")]
        public int CreatedBy { get; set; }

        [JsonProperty("order_count")]
        public int OrderCount { get; set; }

        [JsonProperty("warehouse_id")]
        public int WarehouseId { get; set; }
    }
}