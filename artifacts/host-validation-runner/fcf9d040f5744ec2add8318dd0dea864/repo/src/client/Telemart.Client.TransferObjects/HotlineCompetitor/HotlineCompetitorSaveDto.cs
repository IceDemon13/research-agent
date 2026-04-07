using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.HotlineCompetitor
{
    public class HotlineCompetitorSaveDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("abc_id")]
        public int AbcId { get; set; }

        [JsonProperty("contractor_id")]
        public int? ContractorId { get; set; }
    }
}