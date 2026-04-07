using System.Collections.ObjectModel;

namespace Telemart.Client.ViewModels.Store.ReturnInvoice
{
    public class ReturnInvoiceProductConfirmParameter
    {
        public ReturnInvoiceProductConfirmParameter(int returnInvoiceId, ObservableCollection<ReturnInvoiceProductConfirmViewItem> products)
        {
            Products = products;
            ReturnInvoiceId = returnInvoiceId;
        }

        public ObservableCollection<ReturnInvoiceProductConfirmViewItem> Products { get; private set; }

        public int ReturnInvoiceId { get; set; }
    }
}
