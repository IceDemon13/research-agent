using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.ServiceCenter
{
    public sealed class UnlockServiceCenter : UnlockRequestBase<ServiceCenterDto>
    {
        public UnlockServiceCenter(int id, bool force = false)
            : base(force, ApiResources.ServiceCenters, id)
        {
        }
    }
}
