using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Complaint;

namespace Telemart.Client.Data.Requests.Features.Complaint.Actions
{
    public class LockComplaint : LockRequestBase<ComplaintDto>
    {
        public LockComplaint(int id, bool allowLockedByMe = true, bool checkPermissions = false)
            : base(allowLockedByMe, checkPermissions, ApiResources.Complaints, id)
        {
        }
    }
}
