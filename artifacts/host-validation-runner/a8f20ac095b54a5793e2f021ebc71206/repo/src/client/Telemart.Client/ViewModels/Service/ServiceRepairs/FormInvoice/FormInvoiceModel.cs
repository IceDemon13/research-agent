using System.Collections.ObjectModel;
using System.ComponentModel;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Dictionaries;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Service.ServiceInvoices;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Service.ServiceRepairs.FormInvoice
{
    public class FormInvoiceModel : BindableBase, IDataErrorInfo
    {
        private readonly int warehouseId;
        private readonly int? serviceCenterId;

        public FormInvoiceModel(int warehouseId, int? serviceCenterId)
        {
            this.warehouseId = warehouseId;
            this.serviceCenterId = serviceCenterId;

            SelectedServiceRepair = new ObservableCollection<ServiceRepairViewItem>();
            OpenInvoiceAfterCreation = true;
        }

        public int? ServiceCenterId
        {
            get { return GetProperty(() => ServiceCenterId); }
            set { SetProperty(() => ServiceCenterId, value); }
        }

        public ServiceRepairState State
        {
            get { return GetProperty(() => State); }
            set { SetProperty(() => State, value); }
        }

        public ServiceRequestLocation Location
        {
            get { return GetProperty(() => Location); }
            set { SetProperty(() => Location, value); }
        }

        public int WarehouseId
        {
            get { return GetProperty(() => WarehouseId); }
            set { SetProperty(() => WarehouseId, value); }
        }

        public ObservableCollection<ServiceRepairViewItem> ServiceRepairs
        {
            get { return GetProperty(() => ServiceRepairs); }
            set { SetProperty(() => ServiceRepairs, value); }
        }

        public ObservableCollection<ServiceRepairViewItem> SelectedServiceRepair
        {
            get { return GetProperty(() => SelectedServiceRepair); }
            set { SetProperty(() => SelectedServiceRepair, value); }
        }

        public ObservableCollection<ServiceInvoiceViewItem> ServiceInvoices
        {
            get { return GetProperty(() => ServiceInvoices); }
            set { SetProperty(() => ServiceInvoices, value); }
        }

        public ServiceInvoiceViewItem SelectedServiceInvoice
        {
            get { return GetProperty(() => SelectedServiceInvoice); }
            set { SetProperty(() => SelectedServiceInvoice, value); }
        }

        public ObservableCollection<ValidationResultItem> ValidationItems
        {
            get { return GetProperty(() => ValidationItems); }
            set { SetProperty(() => ValidationItems, value); }
        }

        public Result<ServiceInvoiceDto> Result
        {
            get { return GetProperty(() => Result); }
            set { SetProperty(() => Result, value); }
        }

        public bool OpenInvoiceAfterCreation
        {
            get { return GetProperty(() => OpenInvoiceAfterCreation); }
            set { SetProperty(() => OpenInvoiceAfterCreation, value); }
        }

        #region IDataErrorInfo

        string IDataErrorInfo.Error => string.Empty;

        string IDataErrorInfo.this[string columnName] => IDataErrorInfoHelper.GetErrorText(this, columnName);

        #endregion

        public static void BuildMetadata(MetadataBuilder<FormInvoiceModel> builder)
        {
            builder.Property(x => x.ServiceCenterId).Required(() => Resources.RequiredErrorMessage);
        }

        public void SetInitialData()
        {
            State = ServiceRepairState.Confirmed;
            WarehouseId = warehouseId;
            ServiceCenterId = serviceCenterId;
            Location = ServiceRequestLocation.Warehouse;
        }
    }
}