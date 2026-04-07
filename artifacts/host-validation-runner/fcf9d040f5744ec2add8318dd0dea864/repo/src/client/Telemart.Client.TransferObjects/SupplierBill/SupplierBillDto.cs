using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.SupplierBill
{
    public class SupplierBillDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("supplier_id")]
        public int SupplierId { get; set; }

        [JsonProperty("state_id")]
        public int StateId { get; set; }

        [JsonProperty("currency_id")]
        public int CurrencyId { get; set; }

        [JsonProperty("invoice_carry_id")]
        public int? InvoiceCarryId { get; set; }

        [JsonProperty("invoice_id")]
        public int? InvoiceId { get; set; }

        [JsonProperty("employee_lock_id")]
        public int? EmployeeLockId { get; set; }

        [JsonProperty("edrpou")]
        public string Edrpou { get; set; }

        [JsonProperty("number")]
        public string Number { get; set; }

        [JsonProperty("invoiced_on")]
        public DateTime InvoicedOn { get; set; }

        [JsonProperty("comment")]
        public string Comment { get; set; }

        [JsonProperty("modified_on")]
        public DateTime ModifiedOn { get; set; }

        [JsonProperty("modified_by")]
        public int ModifiedBy { get; set; }

        [JsonProperty("created_on")]
        public DateTime CreatedOn { get; set; }

        [JsonProperty("created_by")]
        public int CreatedBy { get; set; }

        [JsonProperty("employee_lock")]
        public EmployeeSimpleDto EmployeeLock { get; set; }

        [JsonProperty("products")]
        public List<SupplierBillProductDto> Products { get; set; }

        [JsonProperty("documents")]
        public List<SupplierBillDocumentDto> Documents { get; set; }
    }
}