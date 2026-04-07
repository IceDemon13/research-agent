using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.BankPayment;

namespace Telemart.Client.Data.Requests.Features.BankPayment.Actions
{
    public class PayBankPayment : CallEntityActionWithBodyRequestResultBase<BankPaymentResultDto, PayBankPayment.BankPaymentPayDto>
    {
        public PayBankPayment(int bankPaymentId, int orderId)
            : base(bankPaymentId, new BankPaymentPayDto { Id = bankPaymentId, OrderId = orderId }, ApiResources.BankPayments, "pay")
        {
        }

        public class BankPaymentPayDto
        {
            [JsonProperty("id")]
            public int Id { get; set; }

            [JsonProperty("order_id")]
            public int OrderId { get; set; }
        }
    }
}