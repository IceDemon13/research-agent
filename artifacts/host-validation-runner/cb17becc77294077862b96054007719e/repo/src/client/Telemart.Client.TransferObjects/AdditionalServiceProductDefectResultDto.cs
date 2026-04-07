using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class AdditionalServiceProductDefectResultDto : ProductDefectResultDtoBase
    {
        [JsonProperty("additional_service_product")]
        public AdditionalServiceProductDto AdditionalServiceProduct { get; set; }
    }
}