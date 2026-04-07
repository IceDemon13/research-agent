using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.ServiceRequest.Actions
{
    public sealed class RecomplectServiceRequest : CallEntityActionWithBodyRequestResultBase<ServiceRequestDto, AssembledComputerSaveDto>
    {
        public RecomplectServiceRequest(int id, AssembledComputerSaveDto saveDto)
            : base(id, saveDto, ApiResources.ServiceRequests, "recomplect")
        {
        }
    }
}