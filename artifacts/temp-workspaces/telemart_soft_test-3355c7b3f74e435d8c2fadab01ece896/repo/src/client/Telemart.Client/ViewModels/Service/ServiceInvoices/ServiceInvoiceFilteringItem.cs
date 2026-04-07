using System;
using System.Collections.Generic;
using System.Linq;
using Telemart.Client.Data.WebClient;

namespace Telemart.Client.ViewModels.Service.ServiceInvoices
{
    public sealed class ServiceInvoiceFilteringItem : IFilteringItem
    {
        private const string Separator = ",";

        public DateTime? ServiceInvoiceSendAfter { get; set; }

        public DateTime? ServiceInvoiceSendBefore { get; set; }

        public string InvoiceIds { get; set; }

        public List<int> Warehouses { get; set; }

        public List<int> ServiceCenters { get; set; }

        public List<int> States { get; set; }

        public IEnumerable<(string Name, object Value)> BuildParameters()
        {
            if (ServiceInvoiceSendAfter.HasValue)
            {
                yield return ("send_after", ServiceInvoiceSendAfter.Value.ToString("s"));
            }

            if (ServiceInvoiceSendBefore.HasValue)
            {
                yield return ("send_before", ServiceInvoiceSendBefore.Value.ToString("s"));
            }

            if (!string.IsNullOrEmpty(InvoiceIds))
            {
                yield return ("ids", InvoiceIds.Trim());
            }

            if (Warehouses != null && Warehouses.Any())
            {
                yield return ("warehouses", string.Join(Separator, Warehouses));
            }

            if (ServiceCenters != null && ServiceCenters.Any())
            {
                yield return ("service_centers", string.Join(Separator, ServiceCenters));
            }

            if (States != null && States.Any())
            {
                yield return ("states", string.Join(Separator, States));
            }
        }
    }
}