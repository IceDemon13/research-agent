using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.ServiceRequest
{
    public sealed class UpdateServiceRequestComment : UpdateCommentBase<ServiceRequestDto>
    {
        public UpdateServiceRequestComment(int id, string comment)
            : base(id, ApiResources.ServiceRequests, comment)
        {
        }
    }
}
