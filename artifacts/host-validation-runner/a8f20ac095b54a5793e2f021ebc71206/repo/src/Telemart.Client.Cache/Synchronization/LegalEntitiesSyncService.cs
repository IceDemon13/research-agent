using Telemart.Client.Cache.Synchronization.Base;
using Telemart.Client.Data.Requests.Features.LegalEntity;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Cache.Synchronization
{
    public class LegalEntitiesSyncService : SyncServiceBase<LegalEntityDto, int>
    {
        public LegalEntitiesSyncService(ICache cache, IWebClient webClient)
            : base(cache, webClient)
        {
        }

        protected override async Task<IReadOnlyCollection<LegalEntityDto>> GetModifiedItemsAsync(DateTime? modifiedOnAfter)
        {
            List<LegalEntityDto> legalEntityDtos = await WebClient.ExecuteApiRequestAsync(new QueryLegalEntities(modifiedOnAfter));

            return legalEntityDtos;
        }
    }
}