using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed record ExpireDateDto
    {
        [JsonProperty("expire_date")]
        public DateTime? ExpireDate { get; init; }
    }
}