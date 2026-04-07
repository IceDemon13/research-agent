using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Security
{
    public sealed class QueryOperations : QueryEntitiesRequestBase<OperationDto>
    {
        public QueryOperations()
            : base(ApiResources.SecurityOperations)
        {
        }
    }
}
