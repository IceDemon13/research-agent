using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Refund.Actions
{
    public sealed class ConfirmRefund : CallEntityActionRequestResultBase<RefundDto>
    {
        public ConfirmRefund(int id)
            : base(id, ApiResources.Refunds, "confirm")
        {
        }
    }
}