using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telemart.Client.Cache;
using Telemart.Client.Cache.Synchronization.Base;
using Telemart.Client.Data.Requests.Features.Setting;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Jobs
{
    public class SyncCacheJob : BackgroundService
    {
        private readonly IEnumerable<ISyncServiceBase> _syncServices;
        private readonly IWebClient _webClient;
        private readonly ILogger<SyncCacheJob> _logger;
        private readonly ILiteDbConnectionFactory _liteDbConnectionFactory;

        public SyncCacheJob(
            IEnumerable<ISyncServiceBase> syncServices,
            IWebClient webClient,
            ILogger<SyncCacheJob> logger,
            ILiteDbConnectionFactory liteDbConnectionFactory)
        {
            _syncServices = syncServices;
            _webClient = webClient;
            _logger = logger;
            _liteDbConnectionFactory = liteDbConnectionFactory;
        }

        public async Task<bool> ExecuteOnceAsync(CancellationToken cancellationToken)
        {
            if (_liteDbConnectionFactory.Create() == null)
            {
                return false;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return false;
            }

            foreach (ISyncServiceBase syncService in _syncServices)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                await syncService.SyncAsync(cancellationToken);
            }

            return true;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            int syncIntervalSeconds = 60;

            while (true)
            {
                try
                {
                    bool success = await ExecuteOnceAsync(cancellationToken);

                    if (!success)
                    {
                        break;
                    }

                    CacheSettingsDto settings = await _webClient.ExecuteApiRequestAsync(new QueryCacheSettings());

                    syncIntervalSeconds = settings.SyncIntervalSeconds;
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Failed to sync cache");
                }

                await Task.Delay(TimeSpan.FromSeconds(syncIntervalSeconds), cancellationToken);
            }
        }
    }
}