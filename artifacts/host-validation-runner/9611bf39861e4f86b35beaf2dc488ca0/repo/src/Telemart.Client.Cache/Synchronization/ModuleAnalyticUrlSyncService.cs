using Telemart.Client.Cache.Synchronization.Base;
using Telemart.Client.Data.Requests.Features.ModuleAnalyticUrl;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Cache.Synchronization
{
    public class ModuleAnalyticUrlSyncService : SyncServiceBase<ModuleAnalyticUrlDto, int>
    {
        public ModuleAnalyticUrlSyncService(ICache cache, IWebClient webClient)
            : base(cache, webClient)
        {
        }

        protected override async Task<IReadOnlyCollection<ModuleAnalyticUrlDto>> GetModifiedItemsAsync(DateTime? modifiedOnAfter)
        {
            List<ModuleAnalyticUrlDto> dtos = await WebClient.ExecuteApiRequestAsync(new QueryModuleAnalyticUrls(modifiedOnAfter));

            return dtos;
        }
    }
}