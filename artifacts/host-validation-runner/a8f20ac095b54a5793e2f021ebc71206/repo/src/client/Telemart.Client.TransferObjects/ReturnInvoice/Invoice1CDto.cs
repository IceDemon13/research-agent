using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.ReturnInvoice
{
    public class Invoice1CDto
    {
        public Invoice1CDto(
            int[] warehousesIds,
            int[] contractorIds,
            DateTime? invoiceGetAfter,
            DateTime? invoiceGetBefore,
            int[] invoiceStatesIds)
        {
            WarehousesIds = warehousesIds;
            ContractorIds = contractorIds;
            InvoiceGetAfter = invoiceGetAfter;
            InvoiceGetBefore = invoiceGetBefore;
            InvoiceStatesIds = invoiceStatesIds;
        }

        [JsonProperty("warehouses_ids")]
        public int[] WarehousesIds { get; set; }

        [JsonProperty("contractor_ids")]
        public int[] ContractorIds { get; set; }

        [JsonProperty("date_after")]
        public DateTime? InvoiceGetAfter { get; set; }

        [JsonProperty("date_before")]
        public DateTime? InvoiceGetBefore { get; set; }

        [JsonProperty("state_ids")]
        public int[] InvoiceStatesIds { get; set; }
    }
}