using Telemart.Client.Cache.Synchronization.Base;
using Telemart.Client.Data.Requests.Features.Contractor;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Paging;

namespace Telemart.Client.Cache.Synchronization
{
    public sealed class ContractorsSyncService : SyncServiceBase<ContractorDto, int>
    {
        public ContractorsSyncService(ICache cache, IWebClient webClient)
            : base(cache, webClient)
        {
        }

        protected override async Task<IReadOnlyCollection<ContractorDto>> GetModifiedItemsAsync(DateTime? modifiedOnAfter)
        {
            PagedResult<ContractorDto> pagedResult = await WebClient.ExecuteApiRequestAsync(new QueryContractors(modifiedOnAfter: modifiedOnAfter));

            return pagedResult.Data;
        }
    }
}