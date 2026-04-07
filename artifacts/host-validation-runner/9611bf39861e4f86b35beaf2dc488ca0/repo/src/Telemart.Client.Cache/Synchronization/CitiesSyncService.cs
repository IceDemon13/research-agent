using Telemart.Client.Cache.Synchronization.Base;
using Telemart.Client.Data.Requests.Features.City;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects.City;
using Telemart.Client.TransferObjects.Paging;

namespace Telemart.Client.Cache.Synchronization
{
    public sealed class CitiesSyncService : SyncServiceBase<CityDto, int>
    {
        public CitiesSyncService(ICache cache, IWebClient webClient)
            : base(cache, webClient)
        {
        }

        protected override async Task<IReadOnlyCollection<CityDto>> GetModifiedItemsAsync(DateTime? modifiedOnAfter)
        {
            PagedResult<CityDto>? pagedResult = await WebClient.ExecuteApiRequestAsync(new QueryCities(modifiedOnAfter));

            return pagedResult.Data;
        }
    }
}