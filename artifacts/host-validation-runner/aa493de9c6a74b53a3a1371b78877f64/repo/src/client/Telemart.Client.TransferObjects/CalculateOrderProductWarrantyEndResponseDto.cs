using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed record CalculateOrderProductWarrantyEndResponseDto
    {
        [JsonProperty("warranty_end")]
        public DateTime WarrantyEnd { get; init; }
    }
}