using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.TradeIn;

public sealed record TradeInMaxPriceDto
{
    [JsonProperty("id")]
    public int Id { get; init; }

    [JsonProperty("trade_in_id")]
    public int TradeInId { get; init; }

    [JsonProperty("max_price")]
    public decimal MaxPrice { get; init; }

    [JsonProperty("created_on")]
    public DateTime CreatedOn { get; init; }
}