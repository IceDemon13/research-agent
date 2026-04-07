using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Movement
{
    public sealed class QueryMovementProductAttributes : QueryEntitiesRequestBase<ProductAttributesDto>
    {
        public QueryMovementProductAttributes(int movementId)
            : base(ApiResources.Movements, movementId, "products", "attributes")
        {
        }
    }
}