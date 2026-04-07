using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed record CreateMonobankDto
    {
        public CreateMonobankDto(int creditPartCount, decimal sum)
        {
            CreditPartCount = creditPartCount;
            Sum = sum;
        }

        [JsonProperty("credit_part_count")]
        public int CreditPartCount { get; init; }

        [JsonProperty("sum")]
        public decimal Sum { get; init; }
    }
}