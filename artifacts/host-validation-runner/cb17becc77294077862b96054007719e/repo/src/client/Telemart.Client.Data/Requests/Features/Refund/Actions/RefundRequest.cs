using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Refund.Actions
{
    public sealed class RefundRequest : CallEntityActionRequestResultBase<RefundDto>
    {
        public RefundRequest(int id)
            : base(id, ApiResources.Refunds, "refund")
        {
        }
    }
}