using System;
using System.ComponentModel;
using DevExpress.Mvvm;
using Telemart.Client.Common.Utils;
using Telemart.Client.Core.Cloning;
using Telemart.Client.Dictionaries;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Service.ServiceInvoices
{
    public sealed class ServiceInvoiceViewItem : BindableBase, ICloneable, IDataErrorInfo, ILockableEntity
    {
        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public int ServiceCenterId
        {
            get { return GetProperty(() => ServiceCenterId); }
            set { SetProperty(() => ServiceCenterId, value); }
        }

        public ServiceInvoiceState State
        {
            get { return GetProperty(() => State); }
            set { SetProperty(() => State, value); }
        }

        public int WarehouseId
        {
            get { return GetProperty(() => WarehouseId); }
            set { SetProperty(() => WarehouseId, value); }
        }

        public CarryType CarryType
        {
            get { return GetProperty(() => CarryType); }
            set { SetProperty(() => CarryType, value); }
        }

        public int? EmployeeCarrierId
        {
            get { return GetProperty(() => EmployeeCarrierId); }
            set { SetProperty(() => EmployeeCarrierId, value); }
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

        public string Ttn
        {
            get { return GetProperty(() => Ttn); }
            set { SetProperty(() => Ttn, value); }
        }

        public DateTime CreatedOn
        {
            get { return GetProperty(() => CreatedOn); }
            set { SetProperty(() => CreatedOn, value); }
        }

        public int CreatedById
        {
            get { return GetProperty(() => CreatedById); }
            set { SetProperty(() => CreatedById, value); }
        }

        public DateTime? SendDate
        {
            get { return GetProperty(() => SendDate); }
            set { SetProperty(() => SendDate, value); }
        }

        public DateTime? SentOn
        {
            get { return GetProperty(() => SentOn); }
            set { SetProperty(() => SentOn, value); }
        }

        public string NpCourierCallBarcode
        {
            get { return GetProperty(() => NpCourierCallBarcode); }
            set { SetProperty(() => NpCourierCallBarcode, value); }
        }

        public string NpCourierCallInterval
        {
            get { return GetProperty(() => NpCourierCallInterval); }
            set { SetProperty(() => NpCourierCallInterval, value); }
        }

        public int ProductsCount => Products?.Count ?? 0;

        public ObservableRangeCollection<ServiceInvoiceProductViewItem> Products
        {
            get { return GetProperty(() => Products); }
            set { SetProperty(() => Products, value, OnProductsChanged); }
        }

        string IDataErrorInfo.Error => string.Empty;

        string IDataErrorInfo.this[string columnName] => IDataErrorInfoHelper.GetErrorText(this, columnName);

        object ICloneable.Clone()
        {
            return Clone();
        }

        public ServiceInvoiceViewItem Clone()
        {
            return ReflectionObjectCloner.Clone(this);
        }

        private void ProductsCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            RaisePropertyChanged(nameof(ProductsCount));
        }

        private void OnProductsChanged()
        {
            RaisePropertyChanged(nameof(ProductsCount));
            Products.CollectionChanged += ProductsCollectionChanged;
        }
    }
}
