using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using Microsoft.Extensions.Logging;
using Quartz;
using Telemart.Client.Common.Messages;
using Telemart.Client.Data.Requests.Features.Currency;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Jobs
{
    [DisallowConcurrentExecution]
    public sealed class GetCurrencyRatesJob : IJob
    {
        public GetCurrencyRatesJob(IWebClient webClient, IMessenger messenger, ILogger<GetCurrencyRatesJob> logger)
        {
            WebClient = webClient ?? throw new ArgumentNullException(nameof(webClient));
            Messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
            Logger = logger;
        }

        private ILogger<GetCurrencyRatesJob> Logger { get; }

        private IMessenger Messenger { get; }

        private IWebClient WebClient { get; }

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                IReadOnlyCollection<CurrencyTypeRateDto> currencyTypeRates = await WebClient.ExecuteApiRequestAsync(new QueryCurrencyTypeRates());
                Messenger.Send(new UpdateCurrencyRatesMessage(currencyTypeRates));
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to get currencies");
            }
        }
    }
}