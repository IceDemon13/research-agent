using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Refund.Actions
{
    public sealed class CancelRefund : CallEntityActionRequestResultBase<RefundDto>
    {
        public CancelRefund(int id)
            : base(id, ApiResources.Refunds, "cancel")
        {
        }
    }
}