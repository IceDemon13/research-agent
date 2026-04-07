using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class WaitingConfirmReturnInvoiceDto
    {
        [JsonProperty("bitrix_deadline")]
        public DateTime BitrixDeadLine { get; set; }
    }
}