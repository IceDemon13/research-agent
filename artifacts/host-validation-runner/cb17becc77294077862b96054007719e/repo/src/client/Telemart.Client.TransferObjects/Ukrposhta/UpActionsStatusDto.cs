using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Ukrposhta
{
    public sealed class UpActionsStatusDto
    {
        [JsonProperty("barcode")]
        public string Barcode { get; set; }

        [JsonProperty("step")]
        public int Step { get; set; }

        [JsonProperty("date")]
        public string Date { get; set; }

        [JsonProperty("index")]
        public string WarehouseIndex { get; set; }

        [JsonProperty("name")]
        public string WarehouseName { get; set; }

        [JsonProperty("event")]
        public int EventId { get; set; }

        [JsonProperty("eventName")]
        public string EventName { get; set; }

        [JsonProperty("eventReason")]
        public string EventReason { get; set; }

        [JsonProperty("country")]
        public string Country { get; set; }

        [JsonProperty("mailType")]
        public int MainTypeId { get; set; }
    }
}