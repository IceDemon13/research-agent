using System;
using DevExpress.Mvvm;
using Telemart.Client.Business;
using Telemart.Client.Business.Order;
using Telemart.Client.Core.Extensions;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.ViewModels.Store.Order.OrderPaymentInfo;
using Telemart.Common.Localization;

namespace Telemart.Client.ViewModels.Store
{
    public sealed class OrderProductViewItem : BindableBase, IOrderProduct, ILocalіzableEntity
    {
        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public int? ParentRecordId
        {
            get { return GetProperty(() => ParentRecordId); }
            set { SetProperty(() => ParentRecordId, value, () => RaisePropertyChanged(nameof(IsGift))); }
        }

        public int CurrencyId
        {
            get { return GetProperty(() => CurrencyId); }
            set { SetProperty(() => CurrencyId, value); }
        }

        public int ProductTypeId
        {
            get { return GetProperty(() => ProductTypeId); }
            set { SetProperty(() => ProductTypeId, value); }
        }

        public bool AssemblyIncluded
        {
            get { return GetProperty(() => AssemblyIncluded); }
            set { SetProperty(() => AssemblyIncluded, value); }
        }

        public decimal Price
        {
            get { return GetProperty(() => Price); }
            set { SetProperty(() => Price, value); }
        }

        public int CurrencyOutId
        {
            get { return GetProperty(() => CurrencyOutId); }
            set { SetProperty(() => CurrencyOutId, value); }
        }

        public decimal PriceOut
        {
            get { return GetProperty(() => PriceOut); }
            set { SetProperty(() => PriceOut, value); }
        }

        public int ProductId
        {
            get { return GetProperty(() => ProductId); }
            set { SetProperty(() => ProductId, value); }
        }

        public bool IsAdditionalService
        {
            get { return GetProperty(() => IsAdditionalService); }
            set { SetProperty(() => IsAdditionalService, value); }
        }

        public bool IsAdditionalServiceConsumable
        {
            get { return GetProperty(() => IsAdditionalServiceConsumable); }
            set { SetProperty(() => IsAdditionalServiceConsumable, value); }
        }

        public bool IsGift
        {
            get { return GetProperty(() => IsGift); }
            set { SetProperty(() => IsGift, value); }
        }

        public bool ShowAdditionalServiceIcon
        {
            get { return GetProperty(() => ShowAdditionalServiceIcon); }
            set { SetProperty(() => ShowAdditionalServiceIcon, value); }
        }

        public bool ShowAccessoryAdditionalServiceIcon
        {
            get { return GetProperty(() => ShowAccessoryAdditionalServiceIcon); }
            set { SetProperty(() => ShowAccessoryAdditionalServiceIcon, value); }
        }

        public string ProductName
        {
            get { return GetProperty(() => ProductName); }
            set { SetProperty(() => ProductName, value, () => { RaisePropertyChanged(nameof(ProductFullName)); }); }
        }

        public string ProductNameUkr
        {
            get { return GetProperty(() => ProductNameUkr); }
            set { SetProperty(() => ProductNameUkr, value, () => { RaisePropertyChanged(nameof(ProductFullName)); }); }
        }

        public string ProductNameEn
        {
            get { return GetProperty(() => ProductNameEn); }
            set { SetProperty(() => ProductNameEn, value, () => { RaisePropertyChanged(nameof(ProductFullName)); }); }
        }

        public string ProductPrefixRus
        {
            get { return GetProperty(() => ProductPrefixRus); }
            set { SetProperty(() => ProductPrefixRus, value, () => { RaisePropertyChanged(nameof(ProductFullName)); }); }
        }

        public string ProductPrefixUkr
        {
            get { return GetProperty(() => ProductPrefixUkr); }
            set { SetProperty(() => ProductPrefixUkr, value, () => { RaisePropertyChanged(nameof(ProductFullName)); }); }
        }

        public string ProductPrefixEn
        {
            get { return GetProperty(() => ProductPrefixEn); }
            set { SetProperty(() => ProductPrefixEn, value, () => { RaisePropertyChanged(nameof(ProductFullName)); }); }
        }

        public int Quantity
        {
            get { return GetProperty(() => Quantity); }
            set { SetProperty(() => Quantity, value); }
        }

        public int? AssemblyId
        {
            get { return GetProperty(() => AssemblyId); }
            set { SetProperty(() => AssemblyId, value); }
        }

        public int? OrderFolderId
        {
            get { return GetProperty(() => OrderFolderId); }
            set { SetProperty(() => OrderFolderId, value); }
        }

        public int? AssemblyQuantity
        {
            get { return GetProperty(() => AssemblyQuantity); }
            set { SetProperty(() => AssemblyQuantity, value); }
        }

        public OrderProductSource Source
        {
            get { return GetProperty(() => Source); }
            set { SetProperty(() => Source, value, () => { RaisePropertyChanged(nameof(IsOnWarehouse)); }); }
        }

        public DateTime? DeliveryDateTime
        {
            get { return GetProperty(() => DeliveryDateTime); }
            set { SetProperty(() => DeliveryDateTime, value); }
        }

        public OrderProductStatus State
        {
            get { return GetProperty(() => State); }
            set { SetProperty(() => State, value, () => { RaisePropertyChanged(nameof(IsOnWarehouse)); }); }
        }

        public int? OrderWarehouseId
        {
            get { return GetProperty(() => OrderWarehouseId); }
            set { SetProperty(() => OrderWarehouseId, value, () => { RaisePropertyChanged(nameof(IsOnWarehouse)); }); }
        }

        public int? OrderConfirmedBy
        {
            get { return GetProperty(() => OrderConfirmedBy); }
            set { SetProperty(() => OrderConfirmedBy, value); }
        }

        public bool FreeDelivery
        {
            get { return GetProperty(() => FreeDelivery); }
            set { SetProperty(() => FreeDelivery, value); }
        }

        Price IOrderPaymentInfoProduct.Price => new Price(PriceOut, CurrencyOutId);

        Price IOrderPaymentInfoProduct.OriginalPrice => new Price(Price, CurrencyId);

        public string ProductFullName => this.GetLocalName(LocalizableNameType.Ukr);

        public bool IsOnWarehouse => State == OrderProductStatus.Agreed && Source is WarehouseOrderProductSource && OrderWarehouseId == Source.WarehouseId;

        string ILocalіzableEntity.Name => ProductName.GetStringWithPrefix(ProductPrefixRus);

        string ILocalіzableEntity.NameUkr => ProductNameUkr.GetStringWithPrefix(ProductPrefixUkr);

        string ILocalіzableEntity.NameEn => ProductNameEn.GetStringWithPrefix(ProductPrefixEn);
    }
}