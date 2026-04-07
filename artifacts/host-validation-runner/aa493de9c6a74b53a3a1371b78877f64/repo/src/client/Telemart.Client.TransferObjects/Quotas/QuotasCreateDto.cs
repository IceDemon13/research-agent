using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Quotas
{
    public sealed class QuotasCreateDto
    {
        public QuotasCreateDto(DateTime? dateTime, QuotaCreateDto[] quotas)
        {
            QuotaDate = dateTime;
            Quotas = quotas;
        }

        [JsonProperty("quota_date")]
        public DateTime? QuotaDate { get;  }

        [JsonProperty("quotas")]
        public QuotaCreateDto[] Quotas { get; }
    }
}