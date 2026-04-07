using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Terminal
{
    public sealed record TerminalDataDto
    {
        [JsonProperty("rrn")]
        public string Rrn { get; init; }

        [JsonProperty("check_number")]
        public uint CheckNumber { get; init; }

        [JsonProperty("rn")]
        public string Rn { get; init; }

        [JsonProperty("terminal_id")]
        public string TerminalId { get; init; }

        [JsonProperty("merchant_id")]
        public string MerchantId { get; init; }

        [JsonProperty("auth_code")]
        public string AuthCode { get; init; }

        [JsonProperty("pan")]
        public string Pan { get; init; }

        [JsonProperty("issuer_name")]
        public string IssuerName { get; init; }

        [JsonProperty("terminal_mac_address")]
        public string TerminalMacAddress { get; set; }
    }
}