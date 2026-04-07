using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.TradeIn;

public sealed class TradeInMaxPriceGetDto
{
    public TradeInMaxPriceGetDto(int productId)
    {
        ProductId = productId;
    }

    [JsonProperty("product_id")]
    public int ProductId { get; }
}