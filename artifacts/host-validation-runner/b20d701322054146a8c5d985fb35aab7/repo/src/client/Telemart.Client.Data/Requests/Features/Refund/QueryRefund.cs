using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Refund
{
    public sealed class QueryRefund : QueryEntityRequestBase<RefundDto>
    {
        public QueryRefund(int id)
            : base(ApiResources.Refunds, id)
        {
        }
    }
}