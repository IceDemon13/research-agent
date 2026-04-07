using Telemart.Client.Data.Requests.Base.Action;

namespace Telemart.Client.Data.Requests.Features.ExternalPayment
{
    public sealed class CancelExternalPayment : CallEntityActionRequestResultBase<object>
    {
        public CancelExternalPayment(int id)
            : base(id, ApiResources.ExternalPayments, "cancel")
        {
        }
    }
}