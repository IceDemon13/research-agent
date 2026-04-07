using System.Collections.Generic;
using System.Linq;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Telemart.Client.Business;
using Telemart.Client.Common;
using Telemart.Client.Common.Utils;
using Telemart.Client.Core.Cloning;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Store.Order.OrderPaymentInfo;

namespace Telemart.Client.ViewModels.Store.Order.CreateCompleted
{
    public class CreateCompletedOrderProductViewItem : TelemartCloneableViewItemBase, IOrderPaymentInfoProduct
    {
        public CreateCompletedOrderProductViewItem()
        {
            Serials = new ObservableRangeCollection<string>();
        }

        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public int? BonusTypeId
        {
            get { return GetProperty(() => BonusTypeId); }
            set { SetProperty(() => BonusTypeId, value); }
        }

        public int MaxBonusesToUse
        {
            get { return GetProperty(() => MaxBonusesToUse); }
            set { SetProperty(() => MaxBonusesToUse, value); }
        }

        public int AppliedBonusesQuantity
        {
            get { return GetProperty(() => AppliedBonusesQuantity); }
            set { SetProperty(() => AppliedBonusesQuantity, value); }
        }

        public int BonusesToCharge
        {
            get { return GetProperty(() => BonusesToCharge); }
            set { SetProperty(() => BonusesToCharge, value); }
        }

        public decimal Price
        {
            get { return GetProperty(() => Price); }
            set { SetProperty(() => Price, value); }
        }

        public int CurrencyId
        {
            get { return GetProperty(() => CurrencyId); }
            set { SetProperty(() => CurrencyId, value); }
        }

        public decimal PriceOut
        {
            get { return GetProperty(() => PriceOut); }
            set { SetProperty(() => PriceOut, value); }
        }

        public int CurrencyOutId
        {
            get { return GetProperty(() => CurrencyOutId); }
            set { SetProperty(() => CurrencyOutId, value); }
        }

        public OrderProductSource Source
        {
            get { return GetProperty(() => Source); }
            set { SetProperty(() => Source, value, () => RaisePropertiesChanged(nameof(IsOnWarehouse))); }
        }

        public bool IsOnWarehouse => Source is WarehouseOrderProductSource;

        Price IOrderPaymentInfoProduct.OriginalPrice => new Price(Price, CurrencyId);

        Price IOrderPaymentInfoProduct.Price => new Price(PriceOut, CurrencyOutId);

        public int Quantity
        {
            get { return GetProperty(() => Quantity); }
            set { SetProperty(() => Quantity, value, () => RaisePropertiesChanged(nameof(ScannedQuantity), nameof(Deviation), nameof(HasDeviation))); }
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

        public string ProductFullNameUa
        {
            get { return GetProperty(() => ProductFullNameUa); }
            set { SetProperty(() => ProductFullNameUa, value); }
        }

        public IReadOnlyCollection<ProductPriceSimpleDto> Prices
        {
            get { return GetProperty(() => Prices); }
            set { SetProperty(() => Prices, value); }
        }

        public int WarrantyId
        {
            get { return GetProperty(() => WarrantyId); }
            set { SetProperty(() => WarrantyId, value); }
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

        public bool KeepSerialOverridden
        {
            get { return GetProperty(() => KeepSerialOverridden); }
            set { SetProperty(() => KeepSerialOverridden, value); }
        }

        public int? OrderPromoCodeId
        {
            get { return GetProperty(() => OrderPromoCodeId); }
            set { SetProperty(() => OrderPromoCodeId, value); }
        }

        public decimal PromoDiscount
        {
            get { return GetProperty(() => PromoDiscount); }
            set { SetProperty(() => PromoDiscount, value); }
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

        public int? ParentProductId
        {
            get { return GetProperty(() => ParentProductId); }
            set { SetProperty(() => ParentProductId, value); }
        }

        public int? ParentRecordId
        {
            get { return GetProperty(() => ParentRecordId); }
            set { SetProperty(() => ParentRecordId, value); }
        }

        public int ScannedQuantity
        {
            get { return GetProperty(() => ScannedQuantity); }
            set { SetProperty(() => ScannedQuantity, value, () => RaisePropertiesChanged(nameof(Deviation), nameof(HasDeviation))); }
        }

        public int ProductTypeId
        {
            get { return GetProperty(() => ProductTypeId); }
            set { SetProperty(() => ProductTypeId, value); }
        }

        public bool AnyAdditionalServices
        {
            get { return GetProperty(() => AnyAdditionalServices); }
            set { SetProperty(() => AnyAdditionalServices, value); }
        }

        public int Position
        {
            get { return GetProperty(() => Position); }
            set { SetProperty(() => Position, value); }
        }

        public int Deviation => ScannedQuantity - Quantity;

        public bool HasDeviation => Deviation != 0;

        public string LinkRewrite
        {
            get { return GetProperty(() => LinkRewrite); }
            set { SetProperty(() => LinkRewrite, value, () => RaisePropertyChanged(nameof(Url))); }
        }

        public string Url => $"{Constants.ProductBaseUrl}{LinkRewrite}/";

        public override object Clone()
        {
            CreateCompletedOrderProductViewItem item = (CreateCompletedOrderProductViewItem)base.Clone();
            item.Id = 0;
            item.Prices = Prices?.Select(x => ReflectionObjectCloner.Clone(x)).ToArray();
            item.ScannedQuantity = 0;
            item.Serials = new ObservableRangeCollection<string>();
            item.Source = null;

            return item;
        }

        public static void BuildMetadata(MetadataBuilder<CreateCompletedOrderProductViewItem> builder)
        {
            builder.Property(x => x.ScannedQuantity)
                .MatchesInstanceRule((x, y) => x > 0 && x == y.Quantity, () => "Все товары должны быть просканированы");
        }
    }
}