using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Data;
using Telemart.Client.Data.Requests;
using Telemart.Client.Data.Requests.Features.Catalog;
using Telemart.Client.Data.Requests.Features.Prices;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Prices;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.WebClient.Prices
{
    public class PricesWebClient : IPricesClient
    {
        private readonly IRestClientGateway _restClientGateway;
        private readonly IErrorHandler _errorHandler;

        public PricesWebClient(IErrorHandler errorHandler, IRestClientGateway restClientGateway)
        {
            _errorHandler = errorHandler;
            _restClientGateway = restClientGateway;
        }

        public async Task<Result<ProductPricesDto>> QueryPricesByIdsAsync(QueryPricesByIdsRequest request, ISupportServices supportServices, CancellationToken cancellationToken)
        {
            return await _errorHandler.HandleErrorsAsync(_ => ExecutePricesApiRequestAsync(new QueryPricesByIds(request)), "запросе цен", null, supportServices, true, showNotification: false, showError: true, cancellationToken: cancellationToken);
        }

        public async Task<Result<ProductPricesDto>> QuerySimplePricesByIdsAsync(QueryPricesByIdsRequest request, ISupportServices supportServices, CancellationToken cancellationToken)
        {
            return await _errorHandler.HandleErrorsAsync(_ => ExecutePricesApiRequestAsync(new QuerySimplePricesByIds(request)), "запросе цен", null, supportServices, true, showNotification: false, showError: true, cancellationToken: cancellationToken);
        }

        public async Task<Result<ProductPricesDto>> SavePricesAsync(SavePricesRequest request, ISupportServices supportServices)
        {
            return await _errorHandler.HandleErrorsAsync(_ => ExecutePricesApiRequestAsync(new SaveProductPrices(request)), "сохранении цен", null, supportServices, true, showNotification: false, showError: true);
        }

        public async Task<Result<IReadOnlyCollection<ContractorProductExtraChargeDto>>> CalculateExtraChargeAsync(CalculateExtraChargeDto request, ISupportServices supportServices, CancellationToken cancellationToken = default)
        {
            return await _errorHandler.HandleErrorsAsync(
                _ => ExecutePricesApiRequestAsync(
                new CalculateExtraCharge(request)),
                null,
                null,
                supportServices,
                false,
                showNotification: false,
                showError: false,
                showDialog: false,
                cancellationToken: cancellationToken);
        }

        public async Task<Result<IReadOnlyCollection<ProductPriceSaveDto>>> CalculatePricesAsync(CalculatePricesRequest request, ISupportServices supportServices)
        {
            return await _errorHandler.HandleErrorsAsync(_ => ExecutePricesApiRequestAsync(new CalculatePrices(request)), "рассчете цен", "Рассчет произведен", supportServices, true, showNotification: true, showError: true);
        }

        public async Task<Result> SaveRatesAsync(SaveRatesRequest request, ISupportServices supportServices)
        {
            return await _errorHandler.HandleErrorsAsync(_ => ExecutePricesApiRequestAsync(new SaveConversionRates(request)), "сохранении курсов валют", "Курсы сохранены", supportServices, true, showNotification: false, showError: true);
        }

        private Task<T> ExecutePricesApiRequestAsync<T>(IRestClientGatewayRequest<T> request)
            where T : class, new()
        {
            return _restClientGateway.ExecuteAsync(request, Services.Prices);
        }
    }
}