using Telemart.Client.Data.Requests.Base;

namespace Telemart.Client.Data.Requests.Features.Setting
{
    public sealed class QueryBusySecondsAfterCall : QueryEntityRequestBase<string>
    {
        public QueryBusySecondsAfterCall()
            : base(ApiResources.Settings, "busy_seconds_after_call")
        {
        }
    }
}