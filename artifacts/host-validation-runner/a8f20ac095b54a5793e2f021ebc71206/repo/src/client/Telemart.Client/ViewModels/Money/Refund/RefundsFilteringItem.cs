using System;
using System.Collections.Generic;
using Telemart.Client.Data.WebClient;

namespace Telemart.Client.ViewModels.Money.Refund
{
    public class RefundsFilteringItem : FilteringItemBase
    {
        [FilteringItemProperty("created_from")]
        public DateTime? CreatedFrom { get; set; }

        [FilteringItemProperty("created_to")]
        public DateTime? CreatedTo { get; set; }

        [FilteringItemProperty("refund_ids")]
        public string RefundIds { get; set; }

        [FilteringItemProperty("fio")]
        public string Fio { get; set; }

        [FilteringItemProperty("phone")]
        public string Phone { get; set; }

        [FilteringItemProperty("order_ids")]
        public string OrderIds { get; set; }

        [FilteringItemProperty("service_request_ids")]
        public string ServiceRequestIds { get; set; }

        [FilteringItemProperty("payments")]
        public IReadOnlyCollection<int> Payments { get; set; }

        [FilteringItemProperty("cashboxes")]
        public IReadOnlyCollection<int> Cashboxes { get; set; }

        [FilteringItemProperty("states")]
        public IReadOnlyCollection<int> States { get; set; }

        [FilteringItemProperty("completed_on_fiscal_registrar")]
        public bool? CompletedOnFiscalRegistrar { get; set; }
    }
}
