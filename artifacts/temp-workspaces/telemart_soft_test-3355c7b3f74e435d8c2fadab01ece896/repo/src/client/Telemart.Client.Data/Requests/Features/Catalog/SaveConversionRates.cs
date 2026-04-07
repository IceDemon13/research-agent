using System;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.Prices;

namespace Telemart.Client.Data.Requests.Features.Catalog
{
    public sealed class SaveConversionRates : CallActionWithBodyRequestResultBase<object, SaveRatesRequest>
    {
        public SaveConversionRates(SaveRatesRequest request)
            : base(request, "prices", "update-rates")
        {
            DefaultTimeout = TimeSpan.FromMinutes(3);
        }
    }
}