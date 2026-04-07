using DevExpress.Mvvm;
using Telemart.Client.Dictionaries;

namespace Telemart.Client.ViewModels.Service.ServiceInvoices
{
    public sealed class ServiceInvoiceProductViewItem : BindableBase
    {
        public ServiceInvoiceProductViewItem()
        {
            Accepted = true;
        }

        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public ServiceInvoiceProductState State
        {
            get { return GetProperty(() => State); }
            set { SetProperty(() => State, value); }
        }

        public int ServiceRepairId
        {
            get { return GetProperty(() => ServiceRepairId); }
            set { SetProperty(() => ServiceRepairId, value); }
        }

        public int ServiceRequestId
        {
            get { return GetProperty(() => ServiceRequestId); }
            set { SetProperty(() => ServiceRequestId, value); }
        }

        public string RepairInvoice
        {
            get { return GetProperty(() => RepairInvoice); }
            set { SetProperty(() => RepairInvoice, value); }
        }

        public string ProductNameRu
        {
            get { return GetProperty(() => ProductNameRu); }
            set { SetProperty(() => ProductNameRu, value); }
        }

        public string ProductNameUkr
        {
            get { return GetProperty(() => ProductNameUkr); }
            set { SetProperty(() => ProductNameUkr, value); }
        }

        public string SerialNumber
        {
            get { return GetProperty(() => SerialNumber); }
            set { SetProperty(() => SerialNumber, value); }
        }

        public string Defect
        {
            get { return GetProperty(() => Defect); }
            set { SetProperty(() => Defect, value); }
        }

        public bool Accepted
        {
            get { return GetProperty(() => Accepted); }
            set { SetProperty(() => Accepted, value); }
        }

        public string ProductName => string.IsNullOrEmpty(ProductNameUkr) ? ProductNameRu : ProductNameUkr;
    }
}