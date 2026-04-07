using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.MeestExpress
{
    public class MeWarehouseDto
    {
        [JsonProperty("ref")]
        public string Ref { get; set; }

        [JsonProperty("city_ref")]
        public string CityRef { get; set; }

        [JsonProperty("type_id")]
        public int TypeId { get; set; }

        [JsonProperty("number")]
        public int Number { get; set; }

        [JsonProperty("street_type_ru")]
        public string StreetTypeRu { get; set; }

        [JsonProperty("street_description_ru")]
        public string StreetDescriptionRu { get; set; }

        [JsonProperty("house")]
        public string House { get; set; }

        [JsonProperty("flat")]
        public string Flat { get; set; }

        [JsonProperty("weight_limit")]
        public double WeightLimit { get; set; }

        [JsonProperty("active")]
        public bool Active { get; set; }
    }
}