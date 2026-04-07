using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Store.Invoice
{
    public class InvoiceAdditionalCostProductViewItem : TelemartViewItemBase
    {
        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public int InvoiceAdditionalCostId
        {
            get { return GetProperty(() => InvoiceAdditionalCostId); }
            set { SetProperty(() => InvoiceAdditionalCostId, value); }
        }

        public int ProductId
        {
            get { return GetProperty(() => ProductId); }
            set { SetProperty(() => ProductId, value); }
        }

        public string ProductName
        {
            get { return GetProperty(() => ProductName); }
            set { SetProperty(() => ProductName, value); }
        }

        public int Quantity
        {
            get { return GetProperty(() => Quantity); }
            set { SetProperty(() => Quantity, value); }
        }

        public int InvoiceProductId
        {
            get { return GetProperty(() => InvoiceProductId); }
            set { SetProperty(() => InvoiceProductId, value); }
        }

        public decimal Amount
        {
            get { return GetProperty(() => Amount); }
            set { SetProperty(() => Amount, value); }
        }

        public bool Include
        {
            get { return GetProperty(() => Include); }
            set { SetProperty(() => Include, value); }
        }
    }
}
