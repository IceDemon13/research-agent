using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.NovaposhtaTtn
{
    public sealed record NpDocumentUpdateDto
    {
        [JsonProperty("recipient_last_name")]
        public string RecipientLastName { get; init; }

        [JsonProperty("recipient_first_name")]
        public string RecipientFirstName { get; init; }

        [JsonProperty("recipient_middle_name")]
        public string RecipientMiddleName { get; init; }

        [JsonProperty("recipient_phone")]
        public string RecipientPhone { get; init; }

        [JsonProperty("package_weight")]
        public double PackageWeight { get; init; }

        [JsonProperty("package_places")]
        public int PackagePlaces { get; init; }

        [JsonProperty("delivery_data")]
        public DeliveryDataDto DeliveryData { get; init; }

        [JsonProperty("description")]
        public string Description { get; init; }

        [JsonProperty("city_id")]
        public int CityId { get; init; }
    }
}