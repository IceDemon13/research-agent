using Telemart.Client.Cache.Synchronization.Base;
using Telemart.Client.Data.Requests.Features.Novaposhta;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects.Novaposhta;

namespace Telemart.Client.Cache.Synchronization
{
    public sealed class NpContractorsSyncService : SyncServiceBase<NpContractorDto, string>
    {
        public NpContractorsSyncService(ICache cache, IWebClient webClient)
            : base(cache, webClient)
        {
        }

        protected override async Task<IReadOnlyCollection<NpContractorDto>> GetModifiedItemsAsync(DateTime? modifiedOnAfter)
        {
            List<NpContractorDto> npContractorDtos = await WebClient.ExecuteApiRequestAsync(new QueryNpContractors(modifiedOnAfter));

            return npContractorDtos;
        }
    }
}