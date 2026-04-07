using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class ScanSheetCreateRequest
    {
        [JsonProperty("order_ids")]
        public int[] OrderIds { get; set; }

        [JsonProperty("complete_courier_call")]
        public bool? CompleteCourierCallApplication { get; set; }
    }
}