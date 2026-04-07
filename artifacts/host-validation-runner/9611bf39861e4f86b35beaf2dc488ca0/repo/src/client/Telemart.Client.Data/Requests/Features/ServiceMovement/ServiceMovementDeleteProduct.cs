using Telemart.Client.Data.Requests.Base;

namespace Telemart.Client.Data.Requests.Features.ServiceMovement
{
    public sealed class ServiceMovementDeleteProduct : DeleteEntityResultRequestBase<object>
    {
        public ServiceMovementDeleteProduct(int serviceMovementId, int serviceMovementProductId)
            : base(ApiResources.ServiceMovements, serviceMovementId, "products", serviceMovementProductId)
        {
        }
    }
}