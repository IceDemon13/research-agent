using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Tools
{
    public sealed class ChangeState : CallActionWithBodyRequestResultBase<object, ChangeStateDto>
    {
        public ChangeState(ChangeStateDto dto)
            : base(dto, ApiResources.TechSupport, "change_state")
        {
        }
    }
}