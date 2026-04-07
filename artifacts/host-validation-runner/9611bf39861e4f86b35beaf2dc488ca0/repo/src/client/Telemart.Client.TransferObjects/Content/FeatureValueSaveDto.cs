using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Content
{
    public class FeatureValueSaveDto
    {
        public FeatureValueSaveDto(
            int id,
            int featureId,
            string value,
            string valueUkr,
            string valueEn,
            string url,
            string urlUkr,
            string urlEn)
        {
            Id = id;
            FeatureId = featureId;

            Value = value;
            ValueUkr = valueUkr;
            ValueEn = valueEn;

            Url = url;
            UrlUkr = urlUkr;
            UrlEn = urlEn;
        }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("feature_id")]
        public int FeatureId { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }

        [JsonProperty("value_ukr")]
        public string ValueUkr { get; set; }

        [JsonProperty("value_en")]
        public string ValueEn { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("url_ukr")]
        public string UrlUkr { get; set; }

        [JsonProperty("url_en")]
        public string UrlEn { get; set; }
    }
}