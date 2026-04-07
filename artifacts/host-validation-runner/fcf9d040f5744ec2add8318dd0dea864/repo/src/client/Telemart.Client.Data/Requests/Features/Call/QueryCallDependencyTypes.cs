using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Call;

namespace Telemart.Client.Data.Requests.Features.Call
{
    public sealed class QueryCallDependencyTypes : QueryEntitiesRequestBase<CallDependencyTypeDto>
    {
        public QueryCallDependencyTypes()
            : base($"calls/dependency_types")
        {
        }
    }
}