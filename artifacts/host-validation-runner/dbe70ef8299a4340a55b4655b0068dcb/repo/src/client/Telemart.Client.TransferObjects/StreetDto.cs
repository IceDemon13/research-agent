using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class StreetDto
    {
        [JsonProperty("ref")]
        public Guid Ref { get; set; }

        [JsonProperty("city_ref")]
        public Guid CityRef { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("type_name")]
        public string TypeName { get; set; }

        [JsonProperty("active")]
        public bool Active { get; set; }

        public string DisplayName => $"{TypeName} {Name}";
    }
}