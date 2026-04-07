using Telemart.Client.Data.Requests.Base;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects.SupplierCurrency;

namespace Telemart.Client.Data.Requests.Features.SupplierCurrency
{
    public sealed class QuerySupplierCurrencyRateActual : QueryEntitiesRequestBase<SupplierCurrencyRateActualDto>
    {
        public QuerySupplierCurrencyRateActual(IFilteringItem filter)
            : base(filter, $"{ApiResources.SupplierCurrency}/actual")
        {
        }
    }
}