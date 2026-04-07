using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.BankPayment;

namespace Telemart.Client.Data.Requests.Features.BankPayment.Actions
{
    public class IgnoreBankPayment : CallEntityActionRequestResultBase<BankPaymentDto>
    {
        public IgnoreBankPayment(int bankPaymentId)
            : base(bankPaymentId, ApiResources.BankPayments, "ignore")
        {
        }
    }
}