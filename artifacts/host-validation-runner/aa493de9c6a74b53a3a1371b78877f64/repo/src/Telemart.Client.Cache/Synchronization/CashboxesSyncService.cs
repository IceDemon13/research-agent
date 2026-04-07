using Telemart.Client.Cache.Synchronization.Base;
using Telemart.Client.Data.Requests.Features.Cashbox;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects.Cashbox;

namespace Telemart.Client.Cache.Synchronization
{
    public sealed class CashboxesSyncService : SyncServiceBase<CashboxDto, int>
    {
        public CashboxesSyncService(ICache cache, IWebClient webClient)
            : base(cache, webClient)
        {
        }

        protected override async Task<IReadOnlyCollection<CashboxDto>> GetModifiedItemsAsync(DateTime? modifiedOnAfter)
        {
            List<CashboxDto> cashboxes = await WebClient.ExecuteApiRequestAsync(new QueryCashboxes(modifiedOnAfter));

            return cashboxes;
        }
    }
}