using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public record UklonAddressesDto
    {
        [JsonProperty("addresses")]
        public IReadOnlyCollection<UklonAddressDto> Addresses { get; init; }
    }
}