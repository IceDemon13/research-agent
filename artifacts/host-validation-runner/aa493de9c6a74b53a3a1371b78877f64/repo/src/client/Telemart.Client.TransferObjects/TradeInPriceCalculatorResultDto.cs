using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class TradeInPriceCalculatorResultDto
    {
        [JsonProperty("current_tradein_price")]
        public decimal CurrentTradeInPrice { get; set; }
    }
}