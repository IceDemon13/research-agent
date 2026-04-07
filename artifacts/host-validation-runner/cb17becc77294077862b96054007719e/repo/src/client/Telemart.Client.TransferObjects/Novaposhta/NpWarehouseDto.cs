using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Novaposhta
{
    public class NpWarehouseDto
    {
        [JsonProperty("active")]
        public bool Active { get; set; }

        [JsonProperty("address")]
        public string Address { get; set; }

        [JsonProperty("city_ref")]
        public Guid CityRef { get; set; }

        [JsonProperty("number")]
        public int Number { get; set; }

        [JsonProperty("ref")]
        public Guid Ref { get; set; }

        [JsonProperty("type_ref")]
        public Guid TypeRef { get; set; }

        [JsonProperty("phone")]
        public string Phone { get; set; }

        [JsonProperty("total_max_weight_allowed")]
        public double TotalMaxWeightAllowed { get; set; }

        [JsonProperty("place_max_weight_allowed")]
        public double PlaceMaxWeightAllowed { get; set; }

        [JsonProperty("longitude")]
        public string Longitude { get; set; }

        [JsonProperty("latitude")]
        public string Latitude { get; set; }
    }
}