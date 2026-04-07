using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class OrderOptionsDto
    {
        [JsonProperty("separate_warranty_cards")]
        public bool SeparateWarrantyCards { get; set; }

        [JsonProperty("free_delivery")]
        public bool FreeDelivery { get; set; }

        [JsonProperty("dont_call")]
        public bool DontCall { get; set; }

        [JsonProperty("organization_recipient")]
        public bool OrganizationRecipient { get; set; }
    }
}