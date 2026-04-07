using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class BulkCreateBankPaymentDto
    {
        public BulkCreateBankPaymentDto(int cashboxId, List<BulkCreateBankPaymentOrderDto> orders)
        {
            CashboxId = cashboxId;
            Orders = orders;
        }

        [JsonProperty("cashbox_id")]
        public int CashboxId { get; }

        [JsonProperty("orders")]
        public List<BulkCreateBankPaymentOrderDto> Orders { get; }
    }
}