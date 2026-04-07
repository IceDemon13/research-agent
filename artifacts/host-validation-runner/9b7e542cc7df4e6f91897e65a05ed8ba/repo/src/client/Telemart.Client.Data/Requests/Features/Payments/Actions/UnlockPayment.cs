using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Payments;

namespace Telemart.Client.Data.Requests.Features.Payments.Actions
{
    public class UnlockPayment : UnlockRequestBase<PaymentDto>
    {
        public UnlockPayment(int id, bool force = false)
            : base(force, ApiResources.PaymentTypes, id)
        {
        }
    }
}
