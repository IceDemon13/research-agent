using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Payments;

namespace Telemart.Client.Data.Requests.Features.Payments.Actions
{
    public class LockPayment : LockRequestBase<PaymentDto>
    {
        public LockPayment(int id, bool allowLockedByMe = true, bool checkPermissions = false)
            : base(allowLockedByMe, checkPermissions, ApiResources.PaymentTypes, id)
        {
        }
    }
}
