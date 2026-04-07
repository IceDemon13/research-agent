using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Notification
{
    public sealed class NotificationSubscribeConfigDto
    {
        [JsonProperty("category_ids")]
        public IReadOnlyCollection<int> CategoryIds { get; set; }

        [JsonProperty("product_ids")]
        public IReadOnlyCollection<int> ProductIds { get; set; }

        [JsonProperty("preorder_invoice_late_hours")]
        public int? PreorderInvoiceLateHours { get; set; }

        [JsonProperty("invoice_late_hours")]
        public int? InvoiceLateHours { get; set; }

        [JsonProperty("contractor_ids")]
        public IReadOnlyCollection<int> ContractorIds { get; set; }

        [JsonProperty("carry_ids")]
        public IReadOnlyCollection<int> CarryIds { get; set; }

        [JsonProperty("warehouse_ids")]
        public IReadOnlyCollection<int> WarehouseIds { get; set; }
    }
}