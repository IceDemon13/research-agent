using System.Collections.Generic;

namespace Telemart.Client.ViewModels.Service.ServiceInvoices
{
    public sealed class CompleteServiceInvoiceParameter
    {
        public CompleteServiceInvoiceParameter(int serviceInvoiceId, IReadOnlyCollection<ServiceInvoiceProductViewItem> serviceInvoiceProducts)
        {
            ServiceInvoiceId = serviceInvoiceId;
            ServiceInvoiceProducts = serviceInvoiceProducts;
        }

        public int ServiceInvoiceId { get; }

        public IReadOnlyCollection<ServiceInvoiceProductViewItem> ServiceInvoiceProducts { get; }
    }
}
