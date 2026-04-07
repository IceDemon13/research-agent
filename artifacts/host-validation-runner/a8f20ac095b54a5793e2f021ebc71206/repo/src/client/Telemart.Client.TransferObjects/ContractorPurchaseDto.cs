using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class ContractorPurchaseDto
    {
        [JsonProperty("processor_name")]
        public string ProcessorName { get; set; }

        [JsonProperty("login")]
        public string Login { get; set; }

        [JsonProperty("password")]
        public string Password { get; set; }

        [JsonProperty("auto_reserve")]
        public bool AutoReserve { get; set; }

        [JsonProperty("auto_purchase")]
        public bool AutoPurchase { get; set; }

        [JsonProperty("check_unique")]
        public bool CheckUnique { get; set; }
    }
}