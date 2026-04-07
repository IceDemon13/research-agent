using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class GenericAccountsDto
    {
        [JsonProperty("metabase")]
        public GenericMetabaseAccountDto Metabase { get; init; }
    }
}