using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Refund.Actions
{
    public sealed class RevertRefund : CallEntityActionRequestResultBase<RefundDto>
    {
        public RevertRefund(int id)
            : base(id, ApiResources.Refunds, "revert")
        {
        }
    }
}