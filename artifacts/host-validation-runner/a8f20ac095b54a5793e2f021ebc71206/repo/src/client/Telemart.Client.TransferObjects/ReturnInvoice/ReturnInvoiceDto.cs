using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.ReturnInvoice
{
    public sealed class ReturnInvoiceDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("invoice_id")]
        public int InvoiceId { get; set; }

        [JsonProperty("warehouse_id")]
        public int WarehouseId { get; set; }

        [JsonProperty("receiver_city_id")]
        public int? ReceiverCityId { get; set; }

        [JsonProperty("documents_control")]
        public bool DocumentsControl { get; set; }

        [JsonProperty("ready_pack")]
        public bool ReadyPack { get; set; }

        [JsonProperty("supplier_id")]
        public int SupplierId { get; set; }

        [JsonProperty("supplier_name")]
        public string SupplierName { get; set; }

        [JsonProperty("carry_id")]
        public int CarryId { get; set; }

        [JsonProperty("state_id")]
        public int StateId { get; set; }

        [JsonProperty("bitrix_id")]
        public int? BitrixId { get; set; }

        [JsonProperty("return_date")]
        public DateTime? ReturnDate { get; set; }

        [JsonProperty("returned_on")]
        public DateTime? ReturnedOn { get; set; }

        [JsonProperty("created_on")]
        public DateTime CreatedOn { get; set; }

        [JsonProperty("created_by")]
        public int CreatedBy { get; set; }

        [JsonProperty("created_in_ic")]
        public bool? CreatedIn1C { get; set; }

        [JsonProperty("document_1c_out")]
        public string Document1COut { get; set; }

        [JsonProperty("employee_lock_id")]
        public int? EmployeeLockId { get; set; }

        [JsonProperty("employee_lock_name")]
        public string EmployeeLockName { get; set; }

        [JsonProperty("sender_np_contractor_ref")]
        public string SenderNpContractorRef { get; set; }

        [JsonProperty("track_number")]
        public string TrackNumber { get; set; }

        [JsonProperty("delivery_data")]
        public DeliveryDataDto DeliveryData { get; set; }

        [JsonProperty("ttn_payer_type_id")]
        public int? TtnPayerTypeId { get; set; }

        [JsonProperty("products")]
        public List<ReturnInvoiceProductDto> Products { get; set; }
    }
}