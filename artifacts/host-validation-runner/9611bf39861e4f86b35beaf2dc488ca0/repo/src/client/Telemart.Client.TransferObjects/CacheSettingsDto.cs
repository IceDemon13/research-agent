using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class CacheSettingsDto
    {
        [JsonProperty("cache_storage_password")]
        public string CacheStoragePassword { get; init; }

        [JsonProperty("sync_interval_seconds")]
        public int SyncIntervalSeconds { get; init; }
    }
}