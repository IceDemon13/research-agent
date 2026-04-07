using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class MovementDelayProductDto
    {
        public MovementDelayProductDto(int productId, short removeSource)
        {
            ProductId = productId;
            RemoveSource = removeSource;
        }

        [JsonProperty("product_id")]
        public int ProductId { get; set; }

        [JsonProperty("remove_source")]
        public short RemoveSource { get; set; }
    }
}