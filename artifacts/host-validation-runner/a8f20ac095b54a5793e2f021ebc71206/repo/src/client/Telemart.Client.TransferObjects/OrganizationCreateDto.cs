using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class OrganizationCreateDto
    {
        [JsonProperty("ownership_id")]
        public int OwnershipId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("is_vat_payer")]
        public bool IsVatPayer { get; set; }
    }
}