using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.ProductCompatibility;

namespace Telemart.Client.Data.Requests.Features.ProductCompatibility.Actions
{
    public class UnlockProductCompatibility : UnlockRequestBase<ProductCompatibilityDto>
    {
        public UnlockProductCompatibility(int id, bool force = false)
            : base(force, ApiResources.ProductCompatibilities, id)
        {
        }
    }
}
