using System;
using System.Collections.Generic;

namespace Telemart.Client.Reports.ReturnInvoice
{
    public class ReturnInvoiceReportData
    {
        public ReturnInvoiceReportData(int id, DateTime createdOn, int invoiceId, DateTime invoiceReceivedOn, IReadOnlyCollection<ReturnInvoiceProductReportData> products)
        {
            ReturnInvoice = $"Возврат {id} от {createdOn}";
            Invoice = $"Накладная {invoiceId} от {invoiceReceivedOn}";
            Products = products;
        }

        public string ReturnInvoice { get; set; }

        public string Invoice { get; set; }

        public IReadOnlyCollection<ReturnInvoiceProductReportData> Products { get; set; }
    }
}
