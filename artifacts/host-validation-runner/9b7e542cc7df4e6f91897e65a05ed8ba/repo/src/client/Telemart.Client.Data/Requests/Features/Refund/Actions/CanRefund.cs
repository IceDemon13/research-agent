using Telemart.Client.Data.Requests.Base.Action;

namespace Telemart.Client.Data.Requests.Features.Refund.Actions
{
    public class CanRefund : CallEntityActionRequestResultBase<object>
    {
        public CanRefund(int id)
            : base(id, ApiResources.Refunds, "can_refund")
        {
        }
    }
}