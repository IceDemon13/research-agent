using System.Collections.Generic;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.SupplierCurrency;

namespace Telemart.Client.Data.Requests.Features.SupplierCurrency
{
    public sealed class CreateSupplierCurrencyRates : CallActionWithBodyRequestResultBase<List<SupplierCurrencyRateHistoryDto>, SupplierCurrencyRatesCreateDto>
    {
        public CreateSupplierCurrencyRates(SupplierCurrencyRatesCreateDto dto)
            : base(dto, ApiResources.SupplierCurrency, "create_supplier_currency_rates")
        {
        }
    }
}