using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.BankPayment
{
    public class BankPaymentResultDto : OrderPaymentResultDto
    {
        [JsonProperty("bank_payment")]
        public BankPaymentDto BankPayment { get; set; }
    }
}