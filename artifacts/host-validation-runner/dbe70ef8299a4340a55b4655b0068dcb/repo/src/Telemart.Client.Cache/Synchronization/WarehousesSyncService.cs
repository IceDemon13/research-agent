using Telemart.Client.Cache.Synchronization.Base;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.TransferObjects.Warehouse;

namespace Telemart.Client.Cache.Synchronization
{
    public sealed class WarehousesSyncService : SyncServiceBase<WarehouseDto, int>
    {
        public WarehousesSyncService(ICache cache, IWebClient webClient)
            : base(cache, webClient)
        {
        }

        protected override async Task<IReadOnlyCollection<WarehouseDto>> GetModifiedItemsAsync(DateTime? modifiedOnAfter)
        {
            PagedResult<WarehouseDto> pagedResult = await WebClient.ExecuteApiRequestAsync(new QueryWarehouses(modifiedOnAfter));

            return pagedResult.Data;
        }
    }
}