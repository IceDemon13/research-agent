using System.Collections.Generic;

namespace Telemart.Client.ViewModels.Store.Invoice
{
    public sealed class MassInvoiceAcceptParameter
    {
        public MassInvoiceAcceptParameter(IReadOnlyCollection<MassInvoiceAcceptViewItem> invoices)
        {
            Invoices = invoices;
        }

        public IReadOnlyCollection<MassInvoiceAcceptViewItem> Invoices { get; }
    }
}