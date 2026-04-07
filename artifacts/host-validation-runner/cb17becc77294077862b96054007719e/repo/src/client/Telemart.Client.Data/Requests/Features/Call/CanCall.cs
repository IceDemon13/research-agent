using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Call;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.Data.Requests.Features.Call
{
    public sealed class CanCall : QueryEntityRequestBase<Result<CallDto>>
    {
        public CanCall(int id)
            : base(ApiResources.Calls, id, "can_call")
        {
        }
    }
}