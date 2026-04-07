using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.Data.Requests.Features.BankPayment.Actions
{
    public class BulkCreateBankPayment : CallActionWithBodyRequestBase<Result<object>, BulkCreateBankPaymentDto>
    {
        public BulkCreateBankPayment(BulkCreateBankPaymentDto dto)
            : base(dto, ApiResources.BankPayments, "bulk_create")
        {
        }
    }
}