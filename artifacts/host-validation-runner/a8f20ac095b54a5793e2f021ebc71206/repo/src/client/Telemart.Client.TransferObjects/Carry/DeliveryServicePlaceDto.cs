using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Carry
{
    public sealed class DeliveryServicePlaceDto
    {
        [JsonProperty("place_id")]
        public string PlaceId { get; set; }

        [JsonProperty("place_name")]
        public string PlaceName { get; set; }

        [JsonProperty("place_name_ukr")]
        public string PlaceNameUkr { get; set; }

        [JsonProperty("place_name_en")]
        public string PlaceNameEn { get; set; }

        [JsonProperty("city_id")]
        public string CityId { get; set; }

        [JsonProperty("number")]
        public int? Number { get; set; }

        [JsonProperty("total_max_weight_allowed")]
        public double? TotalMaxWeightAllowed { get; set; }

        [JsonProperty("limit_post_finance")]
        public int? LimitPostFinance { get; set; }
    }
}