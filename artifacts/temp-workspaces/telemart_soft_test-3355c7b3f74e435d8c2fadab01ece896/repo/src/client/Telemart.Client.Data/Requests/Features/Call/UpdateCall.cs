using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Call;

namespace Telemart.Client.Data.Requests.Features.Call
{
    public sealed class UpdateCall : UpdateEntityResultRequestBase<CallDto, CallSaveDto>
    {
        public UpdateCall(CallSaveDto dto)
            : base(dto, ApiResources.Calls, dto.Id)
        {
        }
    }
}