using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.ReturnInvoice
{
    public class ReturnInvoiceCreateDto
    {
        [JsonProperty("invoice_id")]
        public int InvoiceId { get; set; }

        [JsonProperty("return_date")]
        public DateTime ReturnDate { get; set; }

        [JsonProperty("warehouse_id")]
        public int WarehouseId { get; set; }

        [JsonProperty("carry_id")]
        public int CarryId { get; set; }

        [JsonProperty("receiver_city_id")]
        public int? ReceiverCityId { get; set; }

        [JsonProperty("ttn_payer_type_id")]
        public int? TtnPayerTypeId { get; set; }

        [JsonProperty("delivery_data")]
        public DeliveryDataDto DeliveryData { get; set; }

        [JsonProperty("products")]
        public List<ReturnInvoiceProductCreateDto> Products { get; set; }
    }
}