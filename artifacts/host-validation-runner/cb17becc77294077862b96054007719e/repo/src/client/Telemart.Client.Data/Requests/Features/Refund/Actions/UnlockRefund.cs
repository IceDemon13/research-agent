using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Refund.Actions
{
    public sealed class UnlockRefund : UnlockRequestBase<RefundDto>
    {
        public UnlockRefund(int id, bool force = false)
            : base(force, ApiResources.Refunds, id)
        {
        }
    }
}
