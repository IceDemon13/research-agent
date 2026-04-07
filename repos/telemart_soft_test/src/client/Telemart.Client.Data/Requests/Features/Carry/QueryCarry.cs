using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Carry;

namespace Telemart.Client.Data.Requests.Features.Carry
{
    public sealed class QueryCarry : QueryEntityRequestBase<CarryDto>
    {
        public QueryCarry(int id)
            : base(ApiResources.Carries, id)
        {
        }
    }
}