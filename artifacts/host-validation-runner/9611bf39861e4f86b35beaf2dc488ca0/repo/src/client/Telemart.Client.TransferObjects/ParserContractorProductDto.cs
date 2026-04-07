using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class ParserContractorProductDto
    {
        [JsonProperty("contractor_id")]
        public int ContractorId { get; set; }

        [JsonProperty("link")]
        public string Link { get; set; }
    }
}