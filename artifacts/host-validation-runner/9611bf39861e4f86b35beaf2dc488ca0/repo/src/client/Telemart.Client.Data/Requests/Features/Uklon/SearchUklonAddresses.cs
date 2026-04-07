using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Uklon
{
    public class SearchUklonAddresses : CallActionWithBodyRequestResultBase<UklonAddressesDto, SearchUklonAddresses.SearchUklonAddressesDto>
    {
        public SearchUklonAddresses(SearchUklonAddressesDto dto)
            : base(dto, $"{ApiResources.Uklon}/addresses", "search")
        {
        }

        public record SearchUklonAddressesDto
        {
            [JsonProperty("address")]
            public required string Address { get; init; }

            [JsonProperty("city_id")]
            public required int CityId { get; init; }
        }
    }
}