using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class AccessoryCategorySaveDto
    {
        public AccessoryCategorySaveDto(
            int id,
            int accessoryId,
            int categoryId,
            AccessoryCategoryFeatureSaveDto[] features)
        {
            Id = id;
            AccessoryId = accessoryId;
            CategoryId = categoryId;
            Features = features;
        }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("accessory_id")]
        public int AccessoryId { get; set; }

        [JsonProperty("category_id")]
        public int CategoryId { get; set; }

        [JsonProperty("accessory_category_features")]
        public AccessoryCategoryFeatureSaveDto[] Features { get; set; }
    }
}