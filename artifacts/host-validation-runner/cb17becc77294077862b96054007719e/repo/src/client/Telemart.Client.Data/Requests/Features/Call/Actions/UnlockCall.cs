using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Call;

namespace Telemart.Client.Data.Requests.Features.Call.Actions
{
    public sealed class UnlockCall : UnlockRequestBase<CallDto>
    {
        public UnlockCall(int id, bool force = false)
            : base(force, ApiResources.Calls, id)
        {
        }
    }
}