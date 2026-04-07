using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class ContractorCreateDto
    {
        [JsonProperty("city_id")]
        public int? CityId { get; set; }

        [JsonProperty("foreign_city_id")]
        public int? ForeignCityId { get; set; }

        [JsonProperty("country_id")]
        public int? CountryId { get; set; }

        [JsonProperty("comment")]
        public string Comment { get; set; }

        [JsonProperty("employee_id")]
        public int EmployeeId { get; set; }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("is_competitor")]
        public bool IsCompetitor { get; set; }

        [JsonProperty("is_retail")]
        public bool IsRetail { get; set; }

        [JsonProperty("is_folder")]
        public bool IsFolder { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("parent_id")]
        public int? ParentId { get; set; }

        [JsonProperty("subdivision_id")]
        public int SubdivisionId { get; set; }

        [JsonProperty("edrpou")]
        public string Edrpou { get; set; }

        [JsonProperty("currency_manual")]
        public bool CurrencyManual { get; set; }

        [JsonProperty("old_client")]
        public bool OldClient { get; set; }

        [JsonProperty("active")]
        public bool Active { get; set; }

        [JsonProperty("purchase")]
        public ContractorPurchaseDto Purchase { get; set; }
    }
}