using Telemart.Client.Cache.Synchronization.Base;
using Telemart.Client.Data.Requests.Features.Category;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Paging;

namespace Telemart.Client.Cache.Synchronization
{
    public class CategoriesSyncService : SyncServiceBase<CategoryDto, int>
    {
        public CategoriesSyncService(ICache cache, IWebClient webClient)
            : base(cache, webClient)
        {
        }

        protected override async Task<IReadOnlyCollection<CategoryDto>> GetModifiedItemsAsync(DateTime? modifiedOnAfter)
        {
            PagedResult<CategoryDto> pagedResult = await WebClient.ExecuteApiRequestAsync(new QueryCategories(modifiedOnAfter));

            return pagedResult.Data;
        }
    }
}