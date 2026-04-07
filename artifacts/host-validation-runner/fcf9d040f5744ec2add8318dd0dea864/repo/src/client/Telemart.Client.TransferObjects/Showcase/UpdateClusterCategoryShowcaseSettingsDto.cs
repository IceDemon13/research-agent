using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Showcase
{
    public sealed record UpdateClusterCategoryShowcaseSettingsDto
    {
        [JsonProperty("allow_set_quantity")]
        public bool AllowSetQuantity { get; init; }
    }
}