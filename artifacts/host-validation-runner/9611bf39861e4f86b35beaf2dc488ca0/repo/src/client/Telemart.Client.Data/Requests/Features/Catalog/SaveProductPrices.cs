using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.Prices;

namespace Telemart.Client.Data.Requests.Features.Catalog
{
    public sealed class SaveProductPrices : CallActionWithBodyRequestResultBase<ProductPricesDto, SavePricesRequest>
    {
        public SaveProductPrices(SavePricesRequest request)
            : base(request, "prices", "save")
        {
        }
    }
}