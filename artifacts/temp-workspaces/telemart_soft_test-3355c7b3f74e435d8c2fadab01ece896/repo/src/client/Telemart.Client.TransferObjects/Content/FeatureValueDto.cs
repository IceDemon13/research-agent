using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Content
{
    public class FeatureValueDto : IEquatable<FeatureValueDto>
    {
        [JsonProperty("id")]
        public int Id { get; init; }

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

        public bool Equals(FeatureValueDto other)
        {
            return other?.Id == Id;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as FeatureValueDto);
        }

        public override int GetHashCode()
        {
            return Id;
        }
    }
}