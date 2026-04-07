using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.ServiceRequest.Actions
{
    public sealed class LockServiceRequest : LockRequestBase<ServiceRequestDto>
    {
        public LockServiceRequest(int id, bool allowLockedByMe = true, bool checkPermissions = false)
            : base(allowLockedByMe, checkPermissions, ApiResources.ServiceRequests, id)
        {
        }
    }
}
