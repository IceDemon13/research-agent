using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Prices
{
    [DataContract]
    public class ProductSearchTemplatePriceDto
    {
        [DataMember(Order = 1)]
        [JsonProperty("search_template_id")]
        public int SearchTemplateId { get; set; }

        [DataMember(Order = 2)]
        [JsonProperty("search_template_name")]
        public string SearchTemplateName { get; set; }

        [DataMember(Order = 3)]
        [JsonProperty("price")]
        public decimal Price { get; set; }

        [DataMember(Order = 4)]
        [JsonProperty("contractor_name")]
        public string ContractorName { get; set; }

        [DataMember(Order = 5)]
        [JsonProperty("contractor_id")]
        public int ContractorId { get; set; }
    }
}