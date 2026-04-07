using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Carry;

namespace Telemart.Client.Data.Requests.Features.Carry
{
    public sealed class QueryCarries : QueryEntitiesRequestBase<CarryDto>
    {
        public QueryCarries()
            : base(ApiResources.Carries)
        {
        }
    }
}