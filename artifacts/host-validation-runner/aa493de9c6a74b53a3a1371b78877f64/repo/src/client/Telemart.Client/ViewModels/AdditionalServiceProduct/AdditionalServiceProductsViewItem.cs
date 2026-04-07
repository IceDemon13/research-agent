using System;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.ViewModels.Base;
using Telemart.Common.Localization;

namespace Telemart.Client.ViewModels.AdditionalServiceProduct
{
    public class AdditionalServiceProductsViewItem : TelemartViewItemBase, ILocalіzableEntity
    {
        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public int OrderId
        {
            get { return GetProperty(() => OrderId); }
            set { SetProperty(() => OrderId, value); }
        }

        public int StateId
        {
            get { return GetProperty(() => StateId); }
            set { SetProperty(() => StateId, value); }
        }

        public int OrderStateId
        {
            get { return GetProperty(() => OrderStateId); }
            set { SetProperty(() => OrderStateId, value); }
        }

        public int? EmployeeLockId
        {
            get { return GetProperty(() => EmployeeLockId); }
            set { SetProperty(() => EmployeeLockId, value); }
        }

        public string EmployeeLockName
        {
            get { return GetProperty(() => EmployeeLockName); }
            set { SetProperty(() => EmployeeLockName, value); }
        }

        public int EmployeeId
        {
            get { return GetProperty(() => EmployeeId); }
            set { SetProperty(() => EmployeeId, value); }
        }

        public string AdditionalServiceName
        {
            get { return GetProperty(() => AdditionalServiceName); }
            set { SetProperty(() => AdditionalServiceName, value); }
        }

        public int? PrimaryAdditionalServiceProductId
        {
            get { return GetProperty(() => PrimaryAdditionalServiceProductId); }
            set { SetProperty(() => PrimaryAdditionalServiceProductId, value); }
        }

        public int AdditionalServiceId
        {
            get { return GetProperty(() => AdditionalServiceId); }
            set { SetProperty(() => AdditionalServiceId, value); }
        }

        public DateTime? Date
        {
            get { return GetProperty(() => Date); }
            set { SetProperty(() => Date, value); }
        }

        public bool Scanned
        {
            get { return GetProperty(() => Scanned); }
            set { SetProperty(() => Scanned, value); }
        }

        public DateTime? OrderDeliveryTimeTo
        {
            get { return GetProperty(() => OrderDeliveryTimeTo); }
            set { SetProperty(() => OrderDeliveryTimeTo, value); }
        }

        public DateTime? CompletedOn
        {
            get { return GetProperty(() => CompletedOn); }
            set { SetProperty(() => CompletedOn, value); }
        }

        public string OrderComment
        {
            get { return GetProperty(() => OrderComment); }
            set { SetProperty(() => OrderComment, value); }
        }

        public int WarehouseId
        {
            get { return GetProperty(() => WarehouseId); }
            set { SetProperty(() => WarehouseId, value); }
        }

        public int OrderWarehouseId
        {
            get { return GetProperty(() => OrderWarehouseId); }
            set { SetProperty(() => OrderWarehouseId, value); }
        }

        public bool KeepSerial
        {
            get { return GetProperty(() => KeepSerial); }
            set { SetProperty(() => KeepSerial, value); }
        }

        public string SerialNumber
        {
            get { return GetProperty(() => SerialNumber); }
            set { SetProperty(() => SerialNumber, value); }
        }

        public int PriorityTypeId
        {
            get { return GetProperty(() => PriorityTypeId); }
            set { SetProperty(() => PriorityTypeId, value); }
        }

        public string Name
        {
            get { return GetProperty(() => Name); }
            set { SetProperty(() => Name, value, () => RaisePropertyChanged(nameof(ProductName))); }
        }

        public string NameUkr
        {
            get { return GetProperty(() => NameUkr); }
            set { SetProperty(() => NameUkr, value, () => RaisePropertyChanged(nameof(ProductName))); }
        }

        public string NameEn
        {
            get { return GetProperty(() => NameEn); }
            set { SetProperty(() => NameEn, value, () => RaisePropertyChanged(nameof(ProductName))); }
        }

        public string ProductName => this.GetLocalName(LocalizableNameType.Ukr);
    }
}