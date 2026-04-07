using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class ProductFeatureGroupDto
    {
        [JsonProperty("gname")]
        public string Name { get; set; }

        [JsonProperty("gname_ukr")]
        public string NameUkr { get; set; }

        [JsonProperty("gname_en")]
        public string NameEn { get; set; }

        [JsonProperty("features")]
        public List<ProductFeatureDto> Attributes { get; set; }
    }
}