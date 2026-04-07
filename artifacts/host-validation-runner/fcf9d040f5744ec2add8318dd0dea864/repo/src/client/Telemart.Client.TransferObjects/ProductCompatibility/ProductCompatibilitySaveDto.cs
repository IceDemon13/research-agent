using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.ProductCompatibility
{
    public sealed class ProductCompatibilitySaveDto
    {
        [JsonProperty("id")]
        public int Id { get; init; }

        [JsonProperty("type_id")]
        public int TypeId { get; init; }

        [JsonProperty("notification_image_id")]
        public int NotificationImageId { get; init; }

        [JsonProperty("slave_category_id")]
        public int SlaveCategoryId { get; init; }

        [JsonProperty("slave_feature_id")]
        public int SlaveFeatureId { get; init; }

        [JsonProperty("compare_method")]
        public string CompareMethod { get; init; }

        [JsonProperty("validation_message_template")]
        public string ValidationMessageTemplate { get; init; }

        [JsonProperty("validation_message_template_ukr")]
        public string ValidationMessageTemplateUkr { get; init; }

        [JsonProperty("validation_message_template_en")]
        public string ValidationMessageTemplateEn { get; init; }

        [JsonProperty("master_category_id")]
        public int MasterCategoryId { get; init; }

        [JsonProperty("master_feature_id")]
        public int MasterFeatureId { get; init; }

        [JsonProperty("active")]
        public bool Active { get; init; }
    }
}