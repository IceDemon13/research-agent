using Telemart.Client.Data.Requests.Base;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects.Call;

namespace Telemart.Client.Data.Requests.Features.Call
{
    public sealed class QueryCalls : QueryEntitiesPagedRequestBase<CallDto>
    {
        public QueryCalls(IFilteringItem filter)
            : base(filter, ApiResources.Calls)
        {
        }
    }
}