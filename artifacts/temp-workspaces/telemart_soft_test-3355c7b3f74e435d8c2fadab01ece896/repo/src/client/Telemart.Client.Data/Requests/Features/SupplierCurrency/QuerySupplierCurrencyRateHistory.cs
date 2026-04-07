using Telemart.Client.Data.Requests.Base;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects.SupplierCurrency;

namespace Telemart.Client.Data.Requests.Features.SupplierCurrency
{
    public sealed class QuerySupplierCurrencyRateHistory : QueryEntitiesRequestBase<SupplierCurrencyRateHistoryDto>
    {
        public QuerySupplierCurrencyRateHistory(IFilteringItem filter)
            : base(filter, $"{ApiResources.SupplierCurrency}/history")
        {
        }
    }
}