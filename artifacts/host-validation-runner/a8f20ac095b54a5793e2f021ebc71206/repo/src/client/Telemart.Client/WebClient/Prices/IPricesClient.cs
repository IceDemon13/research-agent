using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Prices;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.WebClient.Prices
{
    public interface IPricesClient
    {
        Task<Result<ProductPricesDto>> QueryPricesByIdsAsync(QueryPricesByIdsRequest request, ISupportServices supportServices, CancellationToken cancellationToken = default);

        Task<Result<ProductPricesDto>> QuerySimplePricesByIdsAsync(QueryPricesByIdsRequest request, ISupportServices supportServices, CancellationToken cancellationToken = default);

        Task<Result<ProductPricesDto>> SavePricesAsync(SavePricesRequest request, ISupportServices supportServices);

        Task<Result<IReadOnlyCollection<ContractorProductExtraChargeDto>>> CalculateExtraChargeAsync(CalculateExtraChargeDto request, ISupportServices supportServices, CancellationToken cancellationToken = default);

        Task<Result<IReadOnlyCollection<ProductPriceSaveDto>>> CalculatePricesAsync(CalculatePricesRequest request, ISupportServices supportServices);

        Task<Result> SaveRatesAsync(SaveRatesRequest request, ISupportServices supportServices);
    }
}