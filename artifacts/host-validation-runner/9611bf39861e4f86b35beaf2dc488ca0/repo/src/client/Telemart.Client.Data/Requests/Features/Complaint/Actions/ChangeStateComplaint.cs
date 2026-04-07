using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.Complaint;

namespace Telemart.Client.Data.Requests.Features.Complaint.Actions
{
    public class ChangeStateComplaint : CallEntityActionWithBodyRequestResultBase<ComplaintDto, ComplaintChangeStateDto>
    {
        public ChangeStateComplaint(int id, int stateId, string resolution)
            : base(id, new ComplaintChangeStateDto(id, stateId, resolution), ApiResources.Complaints, "change_state")
        {
        }
    }
}
