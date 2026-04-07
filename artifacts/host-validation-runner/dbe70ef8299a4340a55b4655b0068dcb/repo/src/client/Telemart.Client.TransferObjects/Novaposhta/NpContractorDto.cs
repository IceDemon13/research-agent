using Newtonsoft.Json;
using Telemart.Client.TransferObjects.Base;

namespace Telemart.Client.TransferObjects.Novaposhta
{
    public class NpContractorDto : TrackableDtoBase<string>
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("active")]
        public bool Active { get; set; }
    }
}