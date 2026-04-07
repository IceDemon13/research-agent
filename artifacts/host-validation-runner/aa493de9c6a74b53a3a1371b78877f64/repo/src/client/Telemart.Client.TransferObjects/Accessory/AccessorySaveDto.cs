using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Accessory
{
    public class AccessorySaveDto
    {
        public AccessorySaveDto(
            int id,
            int categoryId,
            bool active,
            IReadOnlyCollection<AccessoryCategorySaveDto> categories,
            IReadOnlyCollection<AccessoryFeatureSaveDto> features)
        {
            Id = id;
            CategoryId = categoryId;
            Active = active;
            Categories = categories;
            Features = features;
        }

        public AccessorySaveDto(
            int categoryId,
            bool active,
            IReadOnlyCollection<AccessoryCategorySaveDto> categories,
            IReadOnlyCollection<AccessoryFeatureSaveDto> features)
            : this(0, categoryId, active, categories, features)
        {
        }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("category_id")]
        public int CategoryId { get; set; }

        [JsonProperty("active")]
        public bool Active { get; set; }

        [JsonProperty("categories")]
        public IReadOnlyCollection<AccessoryCategorySaveDto> Categories { get; set; }

        [JsonProperty("features")]
        public IReadOnlyCollection<AccessoryFeatureSaveDto> Features { get; set; }
    }
}