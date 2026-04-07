using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class ProductPriceKindDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("currency_id")]
        public int CurrencyId { get; set; }

        [JsonProperty("can_switch_in_orders")]
        public bool CanSwitchInOrders { get; set; }

        [JsonProperty("configurator")]
        public bool Configurator { get; set; }

        [JsonProperty("price_column")]
        public int? PriceColumn { get; set; }
    }
}