using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class OrdersShippingResult
    {
        [JsonProperty("np_sheet_ref")]
        public string NpSheetRef { get; set; }
    }
}