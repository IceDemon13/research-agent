using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;

namespace Telemart.Client.Data.Requests.Features.PrivatBank
{
    public sealed class CreateSession : CallActionWithBodyRequestResultBase<PrivatBankCreateSessionResponse, CreateSession.PrivatBankCreateSessionRequest>
    {
        public CreateSession(int cashboxId)
            : base(new PrivatBankCreateSessionRequest(cashboxId), "pb", "createSession")
        {
        }

        public class PrivatBankCreateSessionRequest
        {
            public PrivatBankCreateSessionRequest(int cashboxId)
            {
                CashboxId = cashboxId;
            }

            [JsonProperty("cashbox_id")]
            public int CashboxId { get; set; }
        }
    }
}