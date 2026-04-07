using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.ServiceProduct;

namespace Telemart.Client.Data.Requests.Features.ServiceProduct
{
    public sealed class UnlockServiceProduct : UnlockRequestBase<ServiceProductDto>
    {
        public UnlockServiceProduct(int id, bool force = false)
            : base(force, ApiResources.ServiceProducts, id)
        {
        }
    }
}
