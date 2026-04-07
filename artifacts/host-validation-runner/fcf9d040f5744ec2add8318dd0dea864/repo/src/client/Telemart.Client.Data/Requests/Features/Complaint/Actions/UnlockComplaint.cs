using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Complaint;

namespace Telemart.Client.Data.Requests.Features.Complaint.Actions
{
    public class UnlockComplaint : UnlockRequestBase<ComplaintDto>
    {
        public UnlockComplaint(int id, bool force = false)
            : base(force, ApiResources.Complaints, id)
        {
        }
    }
}
