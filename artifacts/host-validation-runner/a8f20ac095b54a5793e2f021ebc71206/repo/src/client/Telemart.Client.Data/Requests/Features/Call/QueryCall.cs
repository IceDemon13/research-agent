using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Call;

namespace Telemart.Client.Data.Requests.Features.Call
{
    public sealed class QueryCall : QueryEntityRequestBase<CallDto>
    {
        public QueryCall(int id)
            : base(ApiResources.Calls, id)
        {
        }
    }
}