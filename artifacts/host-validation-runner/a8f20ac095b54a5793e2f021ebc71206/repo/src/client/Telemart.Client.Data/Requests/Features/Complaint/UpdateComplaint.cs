using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Complaint;

namespace Telemart.Client.Data.Requests.Features.Complaint
{
    public class UpdateComplaint : UpdateEntityResultRequestBase<ComplaintDto, ComplaintSaveDto>
    {
        public UpdateComplaint(int id, ComplaintSaveDto dto)
            : base(dto, ApiResources.Complaints, id)
        {
        }
    }
}
