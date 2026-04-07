using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class OrganizationSaveDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("is_vat_payer")]
        public bool IsVatPayer { get; set; }

        [JsonProperty("legal_address")]
        public string LegalAddress { get; set; }

        [JsonProperty("post_address")]
        public string PostAddress { get; set; }

        [JsonProperty("inn")]
        public string Inn { get; set; }

        [JsonProperty("inn_vat_payer")]
        public string InnVatPayer { get; set; }

        [JsonProperty("okpo")]
        public string Okpo { get; set; }

        [JsonProperty("vat_licence")]
        public string VatLicence { get; set; }

        [JsonProperty("tax_payer_licence")]
        public string TaxPayerLicence { get; set; }

        [JsonProperty("comment")]
        public string Comment { get; set; }
    }
}