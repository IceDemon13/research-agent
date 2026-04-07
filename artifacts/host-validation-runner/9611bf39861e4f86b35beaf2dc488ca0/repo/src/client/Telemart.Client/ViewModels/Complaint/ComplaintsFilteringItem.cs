using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using Telemart.Client.Data.WebClient;

namespace Telemart.Client.ViewModels.Complaint
{
    public class ComplaintsFilteringItem : FilteringItemBase
    {
        [FilteringItemProperty("complaint_ids")]
        public string ComplaintIds { get; set; }

        [FilteringItemProperty("order_ids")]
        public string OrderIds { get; set; }

        [FilteringItemProperty("service_request_ids")]
        public string ServiceRequestIds { get; set; }

        [FilteringItemProperty("trade_in_ids")]
        public string TradeInIds { get; set; }

        [FilteringItemProperty("created_from")]
        public DateTime? CreatedFrom { get; set; }

        [FilteringItemProperty("created_to")]
        public DateTime? CreatedTo { get; set; }

        [FilteringItemProperty("states")]
        public List<int> States { get; set; }

        [FilteringItemProperty("types")]
        public List<int> Types { get; set; }

        [FilteringItemProperty("employee_ids")]
        public List<int> EmployeeIds { get; set; }

        [FilteringItemProperty("fio")]
        public string Fio { get; set; }

        [FilteringItemProperty("phone")]
        public string Phone { get; set; }
    }
}
