using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.PackList
{
    public class PackListDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("modified_on")]
        public DateTime ModifiedOn { get; set; }

        [JsonProperty("modified_by")]
        public int ModifiedBy { get; set; }

        [JsonProperty("created_on")]
        public DateTime CreatedOn { get; set; }

        [JsonProperty("created_by")]
        public int CreatedBy { get; set; }

        [JsonProperty("completed_on")]
        public DateTime? CompletedOn { get; set; }

        [JsonProperty("completed_by")]
        public int? CompletedBy { get; set; }

        [JsonProperty("collected_on")]
        public DateTime? CollectedOn { get; set; }

        [JsonProperty("collected_by")]
        public int? CollectedBy { get; set; }

        [JsonProperty("packager")]
        public int? PackagerEmployeeId { get; set; }

        [JsonProperty("collector")]
        public int? CollectorEmployeeId { get; set; }

        [JsonProperty("pack_list_orders")]
        public PackListOrderDto[] PackListOrders { get; set; }
    }
}