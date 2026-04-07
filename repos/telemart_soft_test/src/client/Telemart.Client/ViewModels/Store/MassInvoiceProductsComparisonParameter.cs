using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Telemart.Client.ViewModels.Store
{
    public class MassInvoiceProductsComparisonParameter
    {
        public MassInvoiceProductsComparisonParameter(ObservableCollection<InvoiceProductViewItem> invoiceProducts, List<int> supplierAllowDocumentsInvoiceIds)
        {
            InvoiceProducts = invoiceProducts;
            SupplierAllowDocumentsInvoiceIds = supplierAllowDocumentsInvoiceIds;
        }

        public ObservableCollection<InvoiceProductViewItem> InvoiceProducts { get; }

        public List<int> SupplierAllowDocumentsInvoiceIds { get; }
    }
}