using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Products
{
    public sealed class QueryProductPickupReasons : QueryEntitiesRequestBase<ProductPickupReasonDto>
    {
        public QueryProductPickupReasons()
            : base($"{ApiResources.Products}/{ApiResources.PickupReasons}")
        {
        }
    }
}