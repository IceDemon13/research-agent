using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.NovaposhtaBill
{
    public class NovaposhtaBillSaveDto
    {
        [JsonProperty("number")]
        public string Number { get; set; }

        [JsonProperty("contractor_ref")]
        public string ContractorRef { get; set; }

        [JsonProperty("invoiced_on")]
        public DateTime InvoicedOn { get; set; }

        [JsonProperty("ttns")]
        public IReadOnlyCollection<NovaposhtaBillTtnDto> Ttns { get; set; }

        [JsonProperty("document")]
        public byte[] Document { get; set; }
    }
}