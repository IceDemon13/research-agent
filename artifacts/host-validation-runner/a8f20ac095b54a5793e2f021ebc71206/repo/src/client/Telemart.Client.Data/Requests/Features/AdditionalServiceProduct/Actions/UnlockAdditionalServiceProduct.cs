using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.AdditionalServiceProduct.Actions
{
    public sealed class UnlockAdditionalServiceProduct : UnlockRequestBase<AdditionalServiceProductDto>
    {
        public UnlockAdditionalServiceProduct(int id, bool force = false)
            : base(force, ApiResources.AdditionalServicesProducts, id)
        {
        }
    }
}