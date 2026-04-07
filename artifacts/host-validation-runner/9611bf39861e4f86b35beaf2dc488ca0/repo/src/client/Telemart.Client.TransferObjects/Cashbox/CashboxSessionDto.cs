using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Cashbox
{
    public class CashboxSessionDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("closed")]
        public bool Closed { get; set; }

        [JsonProperty("amount")]
        public decimal Amount { get; set; }

        [JsonProperty("last_zreport_id")]
        public string LastZreportId { get; set; }
    }
}