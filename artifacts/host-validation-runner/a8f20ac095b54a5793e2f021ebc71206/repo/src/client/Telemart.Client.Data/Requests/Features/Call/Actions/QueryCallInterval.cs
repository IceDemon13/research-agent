using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.Call;

namespace Telemart.Client.Data.Requests.Features.Call.Actions
{
    public sealed class QueryCallInterval : CallActionWithBodyRequestBase<CallIntervalResponse, CallIntervalRequest>
    {
        public QueryCallInterval(int? callTypeId, int attempt)
            : base(new CallIntervalRequest(callTypeId, attempt), ApiResources.Calls, "get_call_interval")
        {
        }
    }
}