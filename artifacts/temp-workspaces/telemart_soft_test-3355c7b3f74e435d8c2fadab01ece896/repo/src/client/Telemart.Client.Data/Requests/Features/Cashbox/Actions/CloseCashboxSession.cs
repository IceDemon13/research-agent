using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.Cashbox;

namespace Telemart.Client.Data.Requests.Features.Cashbox.Actions
{
    public class CloseCashboxSession : CallEntityActionWithBodyRequestResultBase<CashboxDto, CloseCashboxSession.CashboxSessionCloseDto>
    {
        public CloseCashboxSession(int id, decimal amount)
            : base(id, new CashboxSessionCloseDto(id, amount), ApiResources.Cashboxes, "close_session")
        {
        }

        public class CashboxSessionCloseDto
        {
            public CashboxSessionCloseDto(int cashboxId, decimal amount)
            {
                CashboxId = cashboxId;
                Amount = amount;
            }

            [JsonProperty("cashbox_id")]
            public int CashboxId { get; set; }

            [JsonProperty("amount")]
            public decimal Amount { get; set; }
        }
    }
}