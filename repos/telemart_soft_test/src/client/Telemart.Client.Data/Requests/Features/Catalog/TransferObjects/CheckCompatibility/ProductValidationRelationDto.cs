using Newtonsoft.Json;

namespace Telemart.Client.Data.Requests.Features.Catalog.TransferObjects.CheckCompatibility
{
    public sealed class ProductValidationRelationDto
    {
        [JsonProperty("incompatible_with_product_id")]
        public int IncompatibleWithProductId { get; set; }

        [JsonProperty("messages")]
        public string[] Messages { get; set; }

        [JsonProperty("notification_image_id")]
        public int NotificationImageId { get; init; }
    }
}