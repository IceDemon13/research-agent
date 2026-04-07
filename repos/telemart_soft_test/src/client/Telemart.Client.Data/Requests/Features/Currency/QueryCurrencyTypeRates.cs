using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Currency
{
    public sealed class QueryCurrencyTypeRates : QueryEntitiesRequestBase<CurrencyTypeRateDto>
    {
        public QueryCurrencyTypeRates()
            : base(ApiResources.CurrencyTypeRates)
        {
        }
    }
}