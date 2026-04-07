using Newtonsoft.Json;

namespace Telemart.Client.FiscalRegistrar.Responses
{
    public class CashStock
    {
        [JsonProperty("no")]
        public int Id { get; set; }

        [JsonProperty("sum")]
        public decimal Sum { get; set; }
    }
}
