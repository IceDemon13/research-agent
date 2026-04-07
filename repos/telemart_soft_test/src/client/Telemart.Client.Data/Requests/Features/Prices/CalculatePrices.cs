using System.Collections.Generic;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.Prices;

namespace Telemart.Client.Data.Requests.Features.Prices
{
    public sealed class CalculatePrices : CallActionWithBodyRequestResultBase<IReadOnlyCollection<ProductPriceSaveDto>, CalculatePricesRequest>
    {
        public CalculatePrices(CalculatePricesRequest dto)
            : base(dto, ApiResources.Prices, "calculate")
        {
        }
    }
}