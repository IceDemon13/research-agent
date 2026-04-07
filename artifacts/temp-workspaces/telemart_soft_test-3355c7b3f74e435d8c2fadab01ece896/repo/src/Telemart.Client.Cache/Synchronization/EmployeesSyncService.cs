using Telemart.Client.Cache.Synchronization.Base;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Paging;

namespace Telemart.Client.Cache.Synchronization
{
    public sealed class EmployeesSyncService : SyncServiceBase<EmployeeDto, int>
    {
        public EmployeesSyncService(ICache cache, IWebClient webClient)
            : base(cache, webClient)
        {
        }

        protected override async Task<IReadOnlyCollection<EmployeeDto>> GetModifiedItemsAsync(DateTime? modifiedOnAfter)
        {
            PagedResult<EmployeeDto> pagedResult = await WebClient.ExecuteApiRequestAsync(new QueryEmployees(modifiedOnAfter));

            return pagedResult.Data;
        }
    }
}