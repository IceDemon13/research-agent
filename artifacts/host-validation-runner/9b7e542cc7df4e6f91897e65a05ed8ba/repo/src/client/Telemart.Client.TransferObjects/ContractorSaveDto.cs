using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class ContractorSaveDto
    {
        [JsonProperty("active")]
        public bool Active { get; set; }

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

        [JsonProperty("subdivision_id")]
        public int SubdivisionId { get; set; }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("is_supplier")]
        public bool IsSupplier { get; set; }

        [JsonProperty("auto_source")]
        public bool AutoSource { get; set; }

        [JsonProperty("organization")]
        public string Organization { get; set; }

        [JsonProperty("limit")]
        public int Limit { get; set; }

        [JsonProperty("is_service_supplier")]
        public bool IsServiceSupplier { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("parent_id")]
        public int? ParentId { get; set; }

        [JsonProperty("price_type_id")]
        public int PriceTypeId { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("edrpou")]
        public string Edrpou { get; set; }

        [JsonProperty("discount")]
        public decimal? Discount { get; set; }

        [JsonProperty("is_individual")]
        public bool IsIndividual { get; set; }

        [JsonProperty("currency_manual")]
        public bool CurrencyManual { get; set; }

        [JsonProperty("old_client")]
        public bool OldClient { get; set; }

        [JsonProperty("actual_address")]
        public string ActualAddress { get; set; }

        [JsonProperty("legal_address")]
        public string LegalAddress { get; set; }

        [JsonProperty("tin")]
        public string Tin { get; set; }

        [JsonProperty("ownership_form_id")]
        public int? OwnershipFormId { get; set; }

        [JsonProperty("vat_allowed")]
        public bool VatAllowed { get; set; }

        [JsonProperty("allow_documents")]
        public bool AllowDocuments { get; set; }

        [JsonProperty("retail_warranty")]
        public bool RetailWarranty { get; set; }

        [JsonProperty("grace_period")]
        public short GracePeriod { get; set; }

        [JsonProperty("return_period")]
        public short ReturnPeriod { get; set; }

        [JsonProperty("buh_1c_id")]
        public int? Buh1CId { get; set; }

        [JsonProperty("purchase")]
        public ContractorPurchaseDto Purchase { get; set; }

        [JsonProperty("currency_permissions")]
        public ContractorCurrencyPermissionDto[] CurrencyPermissions { get; set; }
    }
}