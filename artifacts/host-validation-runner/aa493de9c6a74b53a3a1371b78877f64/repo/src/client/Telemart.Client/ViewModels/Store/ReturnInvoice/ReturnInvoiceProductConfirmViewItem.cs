using System.Collections.Generic;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Store.ReturnInvoice
{
    public class ReturnInvoiceProductConfirmViewItem : TelemartViewItemBase
    {
        public int ReturnInvoiceProductId
        {
            get { return GetProperty(() => ReturnInvoiceProductId); }
            set { SetProperty(() => ReturnInvoiceProductId, value); }
        }

        public string ProductName
        {
            get { return GetProperty(() => ProductName); }
            set { SetProperty(() => ProductName, value); }
        }

        public int ScannedQuantity
        {
            get { return GetProperty(() => ScannedQuantity); }
            set { SetProperty(() => ScannedQuantity, value); }
        }

        public int ConfirmQuantity
        {
            get { return GetProperty(() => ConfirmQuantity); }
            set { SetProperty(() => ConfirmQuantity, value); }
        }

        public List<string> SerialNumbers
        {
            get { return GetProperty(() => SerialNumbers); }
            set { SetProperty(() => SerialNumbers, value); }
        }
    }
}
