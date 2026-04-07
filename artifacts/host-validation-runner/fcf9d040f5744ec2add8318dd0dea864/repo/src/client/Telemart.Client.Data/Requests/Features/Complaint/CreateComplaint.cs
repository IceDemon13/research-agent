using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Complaint;

namespace Telemart.Client.Data.Requests.Features.Complaint
{
    public class CreateComplaint : CreateEntityResultRequestBase<ComplaintDto, ComplaintCreateDto>
    {
        public CreateComplaint(ComplaintCreateDto dto)
            : base(dto, ApiResources.Complaints)
        {
        }
    }
}
