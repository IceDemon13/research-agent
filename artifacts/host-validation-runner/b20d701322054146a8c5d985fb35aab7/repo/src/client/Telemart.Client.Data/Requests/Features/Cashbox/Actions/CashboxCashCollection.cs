using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.Cashbox;

namespace Telemart.Client.Data.Requests.Features.Cashbox.Actions
{
    public class CashboxCashCollection : CallEntityActionWithBodyRequestResultBase<CashboxDto, CashboxCashCollection.CashboxCashCollectionDto>
    {
        public CashboxCashCollection(int id, decimal amount)
            : base(id, new CashboxCashCollectionDto(id, amount), ApiResources.Cashboxes, "cash_collection")
        {
        }

        public class CashboxCashCollectionDto
        {
            public CashboxCashCollectionDto(int cashboxId, decimal amount)
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