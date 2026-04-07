using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.LogisticsAnalitics
{
    public sealed class LogisticsAnaliticsDto
    {
        [JsonProperty("invoices")]
        public IReadOnlyCollection<LogisticsAnaliticsInvoiceDto> Invoices { get; set; }

        [JsonProperty("movements")]
        public IReadOnlyCollection<LogisticsAnaliticsMovementDto> Movements { get; set; }

        [JsonProperty("orders")]
        public IReadOnlyCollection<LogisticsAnaliticsOrderDto> Orders { get; set; }
    }
}