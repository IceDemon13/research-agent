using System.Collections.Generic;
using System.ComponentModel;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Common.Utils;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Common.Localization;

namespace Telemart.Client.ViewModels.Store.Order
{
    public sealed class OrderProductPackViewItem : BindableBase, IDataErrorInfo, ILocalіzableEntity
    {
        public OrderProductPackViewItem()
        {
            Serials = new ObservableRangeCollection<string>();
        }

        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public int ProductId
        {
            get { return GetProperty(() => ProductId); }
            set { SetProperty(() => ProductId, value); }
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

        public string ProductFullNameUa
        {
            get { return GetProperty(() => ProductFullNameUa); }
            set { SetProperty(() => ProductFullNameUa, value); }
        }

        public int Quantity
        {
            get { return GetProperty(() => Quantity); }
            set { SetProperty(() => Quantity, value, () => RaisePropertiesChanged(nameof(Deviation), nameof(HasDeviation))); }
        }

        public int ProductTypeId
        {
            get { return GetProperty(() => ProductTypeId); }
            set { SetProperty(() => ProductTypeId, value); }
        }

        public bool PrintWarrantyCard
        {
            get { return GetProperty(() => PrintWarrantyCard); }
            set { SetProperty(() => PrintWarrantyCard, value); }
        }

        public ObservableRangeCollection<string> Serials
        {
            get { return GetProperty(() => Serials); }
            set { SetProperty(() => Serials, value); }
        }

        public bool KeepSerial
        {
            get { return GetProperty(() => KeepSerial); }
            set { SetProperty(() => KeepSerial, value); }
        }

        public decimal ScannedQuantity
        {
            get { return GetProperty(() => ScannedQuantity); }
            set { SetProperty(() => ScannedQuantity, value, () => RaisePropertiesChanged(nameof(Deviation), nameof(HasDeviation))); }
        }

        public int WarrantyId
        {
            get { return GetProperty(() => WarrantyId); }
            set { SetProperty(() => WarrantyId, value); }
        }

        public bool KeepSerialOverridden
        {
            get { return GetProperty(() => KeepSerialOverridden); }
            set { SetProperty(() => KeepSerialOverridden, value); }
        }

        public int? AssemblyId
        {
            get { return GetProperty(() => AssemblyId); }
            set { SetProperty(() => AssemblyId, value); }
        }

        public int? AdditionalServiceProductId
        {
            get { return GetProperty(() => AdditionalServiceProductId); }
            set { SetProperty(() => AdditionalServiceProductId, value); }
        }

        public string NomenclatureSeries
        {
            get { return GetProperty(() => NomenclatureSeries); }
            set { SetProperty(() => NomenclatureSeries, value); }
        }

        public bool IsConsumableAdditionalServiceProduct
        {
            get { return GetProperty(() => IsConsumableAdditionalServiceProduct); }
            set { SetProperty(() => IsConsumableAdditionalServiceProduct, value); }
        }

        public bool IsAdditionalServiceProduct
        {
            get { return GetProperty(() => IsAdditionalServiceProduct); }
            set { SetProperty(() => IsAdditionalServiceProduct, value); }
        }

        public string GroupString
        {
            get { return GetProperty(() => GroupString); }
            set { SetProperty(() => GroupString, value); }
        }

        public IReadOnlyCollection<OrderProductQuantityViewItem> OrderProducts
        {
            get { return GetProperty(() => OrderProducts); }
            set { SetProperty(() => OrderProducts, value); }
        }

        public int Deviation => (int)ScannedQuantity - Quantity;

        public bool HasDeviation => Deviation != 0;

        public string ProductName => this.GetLocalName(LocalizableNameType.Ukr);

        #region IDataErrorInfo

        string IDataErrorInfo.Error => string.Empty;

        string IDataErrorInfo.this[string columnName] => IDataErrorInfoHelper.GetErrorText(this, columnName);

        #endregion

        public static void BuildMetadata(MetadataBuilder<OrderProductPackViewItem> builder)
        {
            builder.Property(x => x.ScannedQuantity).InRange(0, 10000);
        }
    }
}