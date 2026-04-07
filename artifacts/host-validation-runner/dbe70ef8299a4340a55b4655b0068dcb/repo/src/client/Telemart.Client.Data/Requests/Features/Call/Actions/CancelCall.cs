using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.Call;

namespace Telemart.Client.Data.Requests.Features.Call.Actions
{
    public sealed class CancelCall : CallEntityActionWithBodyRequestResultBase<CallDto, string>
    {
        public CancelCall(int id, string reason)
            : base(id, reason, ApiResources.Calls, "cancel")
        {
        }
    }
}