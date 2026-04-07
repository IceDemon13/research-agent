using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed record CreateFiscalDocumentSellDto
    {
        [JsonProperty("order_id")]
        public int OrderId { get; init; }

        [JsonProperty("cashbox_id")]
        public int CashboxId { get; init; }

        [JsonProperty("employee_id")]
        public int EmployeeId { get; init; }
    }
}