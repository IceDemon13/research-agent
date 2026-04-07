using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Catalog
{
    public class ProductMovePropertyDto
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("override_from_category")]
        public bool OverrideFromCategory { get; set; }
    }
}