using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.BankPayment;

namespace Telemart.Client.Data.Requests.Features.PrivatBank
{
    public sealed class ImportPaymentsAutoclient : CallActionWithBodyRequestResultBase<BankPaymentDto[], ImportPaymentsAutoclient.PrivatBankImportRequest>
    {
        public ImportPaymentsAutoclient(int cashboxId)
            : base(new PrivatBankImportRequest(cashboxId), "pb", "autoclient/import")
        {
        }

        public class PrivatBankImportRequest
        {
            public PrivatBankImportRequest(int cashboxId)
            {
                CashboxId = cashboxId;
            }

            [JsonProperty("cashbox_id")]
            public int CashboxId { get; set; }
        }
    }
}
