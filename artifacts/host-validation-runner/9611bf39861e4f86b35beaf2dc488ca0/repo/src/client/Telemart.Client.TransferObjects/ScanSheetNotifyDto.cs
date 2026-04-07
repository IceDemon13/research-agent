using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed record ScanSheetNotifyDto
    {
        public ScanSheetNotifyDto(IReadOnlyCollection<int> orderIds)
        {
            OrderIds = orderIds;
        }

        [JsonProperty("order_ids")]
        public IReadOnlyCollection<int> OrderIds { get; }
    }
}