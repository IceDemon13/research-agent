using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.BankPayment;

namespace Telemart.Client.Data.Requests.Features.PrivatBank
{
    public sealed class ImportPayments : CallActionWithBodyRequestResultBase<BankPaymentDto[], ImportPayments.PrivatBankImportRequest>
    {
        public ImportPayments(string sessionId, int cashboxId)
            : base(new PrivatBankImportRequest(sessionId, cashboxId), "pb", "import")
        {
        }

        public class PrivatBankImportRequest
        {
            public PrivatBankImportRequest(string sessionId, int cashboxId)
            {
                SessionId = sessionId;
                CashboxId = cashboxId;
            }

            [JsonProperty("session_id")]
            public string SessionId { get; set; }

            [JsonProperty("cashbox_id")]
            public int CashboxId { get; set; }
        }
    }
}