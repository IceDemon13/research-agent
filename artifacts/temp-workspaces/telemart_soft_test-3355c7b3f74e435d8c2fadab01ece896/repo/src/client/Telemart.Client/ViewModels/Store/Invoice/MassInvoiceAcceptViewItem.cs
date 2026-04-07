using Telemart.Client.Common.Utils;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Store.Invoice
{
    public class MassInvoiceAcceptViewItem : TelemartViewItemBase
    {
        public int InvoiceId
        {
            get { return GetProperty(() => InvoiceId); }
            set { SetProperty(() => InvoiceId, value); }
        }

        public bool SupplierAllowDocuments
        {
            get { return GetProperty(() => SupplierAllowDocuments); }
            set { SetProperty(() => SupplierAllowDocuments, value); }
        }

        public string SupplierName
        {
            get { return GetProperty(() => SupplierName); }
            set { SetProperty(() => SupplierName, value); }
        }

        public string SupplierOrganization
        {
            get { return GetProperty(() => SupplierOrganization); }
            set { SetProperty(() => SupplierOrganization, value); }
        }

        public ObservableRangeCollection<InvoiceProductViewItem> InvoiceProducts
        {
            get { return GetProperty(() => InvoiceProducts); }
            set { SetProperty(() => InvoiceProducts, value); }
        }
    }
}