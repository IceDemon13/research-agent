using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects.Base;

namespace Telemart.Client.Cache.Synchronization.Base
{
    public abstract class SyncServiceBase<TDto, TIdentity> : ISyncServiceBase
    where TDto : TrackableDtoBase<TIdentity>
    {
        private readonly ICache _cache;
        private long _syncIncrement;

        protected SyncServiceBase(ICache cache, IWebClient webClient)
        {
            _cache = cache;
            WebClient = webClient;
        }

        protected IWebClient WebClient { get; }

        protected abstract Task<IReadOnlyCollection<TDto>> GetModifiedItemsAsync(DateTime? modifiedOnAfter);

        public async Task SyncAsync(CancellationToken cancellationToken)
        {
            _syncIncrement++;
            
            bool fullSync = _syncIncrement % 5 == 0;
            
            DateTime? lastSyncDate = await _cache.GetLastSyncDateAsync<TDto>();

            IReadOnlyCollection<TDto> modifiedItems = await GetModifiedItemsAsync(fullSync ? null : lastSyncDate);

            if (modifiedItems?.Any() != true)
            {
                return;
            }

            await _cache.UpsertAsync(modifiedItems);

            if (fullSync)
            {
                HashSet<TIdentity> modifiedItemIds = modifiedItems.Select(x => x.Id).ToHashSet();
            
                TIdentity[] currentCacheItemIds = (await _cache.GetAllAsync<TDto>())?.Select(x => x.Id).ToArray() ?? Array.Empty<TIdentity>();

                TIdentity[] itemIdsFromLocalCacheToDelete = currentCacheItemIds.Where(x => !modifiedItemIds.Contains(x)).ToArray();
                
                await _cache.DeleteManyAsync<TDto>(x => itemIdsFromLocalCacheToDelete.Contains(x.Id));
            }
            
            await _cache.SetLastSyncDateAsync<TDto>(modifiedItems.Max(x => x.ModifiedOn));
        }
    }
}