using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Telemart.Fiscal.Client.TransferObjects;

namespace Telemart.Client.TransferObjects.FiscalDocument
{
    public class FiscalDocumentDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("number")]
        public string Number { get; set; }

        [JsonProperty("cashbox_id")]
        public int CashboxId { get; set; }

        [JsonProperty("entity_id")]
        public int? EntityId { get; set; }

        [JsonProperty("entity_type_id")]
        public int? EntityTypeId { get; set; }

        [JsonProperty("type_id")]
        public int TypeId { get; set; }

        [JsonProperty("amount")]
        public decimal Amount { get; set; }

        [JsonProperty("created_on")]
        public DateTime CreatedOn { get; set; }

        [JsonProperty("created_by")]
        public int CreatedBy { get; set; }

        [JsonProperty("products")]
        public IReadOnlyCollection<FiscalDocumentProductDto> Products { get; set; }

        [JsonProperty("payments")]
        public IReadOnlyCollection<FiscalDocumentPaymentDto> Payments { get; set; }
    }
}