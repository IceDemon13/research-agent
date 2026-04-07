using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class AdditionalServiceProductClientProductReportDataDto
    {
        [JsonProperty("full_name_client")]
        public string FullNameClient { get; init; }

        [JsonProperty("phone_number")]
        public string PhoneNumber { get; init; }

        [JsonProperty("place_name")]
        public string PlaceName { get; init; }

        [JsonProperty("guest_products")]
        public GuestProductReportDataDto[] GuestProducts { get; set; }
    }
}