using System.Collections.Generic;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Common.Messages
{
    public sealed class UpdateCurrencyRatesMessage
    {
        public UpdateCurrencyRatesMessage(IReadOnlyCollection<CurrencyTypeRateDto> currencyTypeRates)
        {
            CurrencyTypeRates = currencyTypeRates;
        }

        public IReadOnlyCollection<CurrencyTypeRateDto> CurrencyTypeRates { get; }
    }
}