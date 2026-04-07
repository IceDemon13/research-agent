using System;
using Telemart.Client.Data.WebClient;

namespace Telemart.Client.ViewModels.TradeIn
{
    public sealed class TradeInFilteringItem : FilteringItemBase
    {
        [FilteringItemProperty("fio")]
        public string Fio { get; set; }

        [FilteringItemProperty("phone")]
        public string Phone { get; set; }

        [FilteringItemProperty("class_ids")]
        public string ClassIds { get; set; }

        [FilteringItemProperty("state_ids")]
        public string StateIds { get; set; }

        [FilteringItemProperty("ids")]
        public string Ids { get; set; }

        [FilteringItemProperty("service_request_ids")]
        public string ServiceRequestIds { get; set; }

        [FilteringItemProperty("created_on_before")]
        public DateTime? CreatedOnBefore { get; set; }

        [FilteringItemProperty("created_on_after")]
        public DateTime? CreatedOnAfter { get; set; }

        [FilteringItemProperty("completed_on_before")]
        public DateTime? CompletedOnBefore { get; set; }

        [FilteringItemProperty("completed_on_after")]
        public DateTime? CompletedOnAfter { get; set; }

        [FilteringItemProperty("evaluated_on_before")]
        public DateTime? EvaluatedOnBefore { get; set; }

        [FilteringItemProperty("evaluated_on_after")]
        public DateTime? EvaluatedOnAfter { get; set; }

        [FilteringItemProperty("created_by")]
        public int? CreatedBy { get; set; }

        [FilteringItemProperty("evaluated_by")]
        public int? EvaluatedBy { get; set; }

        [FilteringItemProperty("tested_by")]
        public int? TestedBy { get; set; }

        [FilteringItemProperty("completed_by")]
        public int? CompletedBy { get; set; }

        [FilteringItemProperty("product_id")]
        public int? ProductId { get; set; }
    }
}