using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using DevExpress.XtraEditors.DXErrorProvider;
using Telemart.Client.Business;
using Telemart.Client.Business.Order;
using Telemart.Client.Common;
using Telemart.Client.Common.Services;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Promo;
using Telemart.Client.ViewModels.Store.Order.OrderPaymentInfo;
using Telemart.Common.Helpers;

namespace Telemart.Client.ViewModels.Store.Order
{
    public sealed class OrderProductViewModel : ViewModelBase, IDXDataErrorInfo, IDataErrorInfo, IOrderProduct
    {
        public OrderProductViewModel(IDictionaries dictionaries, IMessageFacadeService messageFacadeService, IOrderRules orderRules)
        {
            OrderRules = orderRules ?? throw new ArgumentNullException(nameof(orderRules));
            MessageFacadeService = messageFacadeService ?? throw new ArgumentNullException(nameof(messageFacadeService));
            Dictionaries = dictionaries ?? throw new ArgumentNullException(nameof(dictionaries));
        }

        public event Action ValidationStarted;

        public int CurrencyOutId
        {
            get { return GetProperty(() => CurrencyOutId); }
            set { SetProperty(() => CurrencyOutId, value, CurrencyOutIdChangedCallback); }
        }

        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value, () => RaisePropertiesChanged(nameof(AssembliesInAssemblyModule), nameof(CompletedAdditionalServiceProductsQuantity))); }
        }

        public int? ParentRecordId
        {
            get { return GetProperty(() => ParentRecordId); }
            set { SetProperty(() => ParentRecordId, value); }
        }

        public int? AdditionalServiceId
        {
            get { return GetProperty(() => AdditionalServiceId); }
            set { SetProperty(() => AdditionalServiceId, value); }
        }

        public decimal? AdditionalServicePercent
        {
            get { return GetProperty(() => AdditionalServicePercent); }
            set { SetProperty(() => AdditionalServicePercent, value); }
        }

        public decimal? AdditionalServiceMinPrice
        {
            get { return GetProperty(() => AdditionalServiceMinPrice); }
            set { SetProperty(() => AdditionalServiceMinPrice, value); }
        }

        public int? OrderFolderId
        {
            get { return GetProperty(() => OrderFolderId); }
            set { SetProperty(() => OrderFolderId, value); }
        }

        public bool AnyAdditionalServices
        {
            get { return GetProperty(() => AnyAdditionalServices); }
            set { SetProperty(() => AnyAdditionalServices, value); }
        }

        public bool AnyAdditionalServiceProvideProducts
        {
            get { return GetProperty(() => AnyAdditionalServiceProvideProducts); }
            set { SetProperty(() => AnyAdditionalServiceProvideProducts, value); }
        }

        public int? ParentId
        {
            get { return GetProperty(() => ParentId); }
            set { SetProperty(() => ParentId, value, () => RaisePropertyChanged(nameof(IsVirtualProduct))); }
        }

        public bool IsGift
        {
            get { return GetProperty(() => IsGift); }
            set { SetProperty(() => IsGift, value); }
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

        public bool IsNew => Id < 0;

        public decimal Price1C { get; set; }

        public bool PriceCanBeChanged => OrderRules.IsProductPriceCanBeChanged(this)
                                         && ParentOrder.IsLockedByCurrentUserAndEditingAllowed
                                         && !IsVirtualProduct
                                         && (!ParentOrder.ExternalPayments.Any()
                                             || ParentOrder.ExternalPayments.All(x => Payment.IsEditingAllowed(x.Payment.Id, x.PaymentStateId, x.Payment?.Credit, x.Payment?.PartialCredit)))
                                         && ParentOrder?.Bonuses?.Any(x => x.OrderProductId == Id) != true;

        public decimal PriceOut
        {
            get { return GetProperty(() => PriceOut); }
            set { SetProperty(() => PriceOut, value, PriceChangedCallback); }
        }

        public int? PriceIdOld
        {
            get { return GetProperty(() => PriceIdOld); }
            set { SetProperty(() => PriceIdOld, value); }
        }

        public int? PriceId
        {
            get { return GetProperty(() => PriceId); }
            set { SetProperty(() => PriceId, value, PriceIdChanged); }
        }

        public decimal? InvoicePrice
        {
            get { return GetProperty(() => InvoicePrice); }
            set { SetProperty(() => InvoicePrice, value, () => RaisePropertyChanged(nameof(PriceOut))); }
        }

        public decimal PromoDiscount
        {
            get { return GetProperty(() => PromoDiscount); }
            set { SetProperty(() => PromoDiscount, value); }
        }

        public int? InitiatedBy
        {
            get { return GetProperty(() => InitiatedBy); }
            set { SetProperty(() => InitiatedBy, value); }
        }

        public decimal? UnavailableOrderProductPrice
        {
            get { return GetProperty(() => UnavailableOrderProductPrice); }
            set { SetProperty(() => UnavailableOrderProductPrice, value); }
        }

        public int? CreatedBy
        {
            get { return GetProperty(() => CreatedBy); }
            set { SetProperty(() => CreatedBy, value); }
        }

        public DateTime? CreatedOn
        {
            get { return GetProperty(() => CreatedOn); }
            set { SetProperty(() => CreatedOn, value); }
        }

        public ProductSimpleDto UnavailableProduct
        {
            get { return GetProperty(() => UnavailableProduct); }
            set { SetProperty(() => UnavailableProduct, value); }
        }

        public string UnavailableProductLink => UnavailableProduct != null ? $"/{UnavailableProduct.ParentLinkRewrite}/?p={UnavailableProduct.Id},{Product.Id}" : string.Empty;

        public ProductSimpleDto Product
        {
            get { return GetProperty(() => Product); }
            set { SetProperty(() => Product, value, () => RaisePropertyChanged(nameof(IsGuestProduct))); }
        }

        public int ProductId => Product.Id;

        public string ProductName => Product.GetLocalName(LocalizableNameType.Ukr);

        public CatalogPromoSimpleDto Promo { get; set; }

        public OrderFolderDto OrderFolder { get; set; }

        public decimal Price { get; set; }

        public int CurrencyId { get; set; }

        public int? InvoiceId { get; set; }

        public int? OrderPromoCodeId { get; set; }

        public int Quantity
        {
            get { return GetProperty(() => Quantity); }
            set { SetProperty(() => Quantity, value, QuantityChangedCallback); }
        }

        public int? AssemblyId
        {
            get { return GetProperty(() => AssemblyId); }
            set { SetProperty(() => AssemblyId, value); }
        }

        public int? AssemblyQuantity
        {
            get { return GetProperty(() => AssemblyQuantity); }
            set { SetProperty(() => AssemblyQuantity, value, AssemblyQuantityChangedCallback); }
        }

        public bool AssemblyIncluded
        {
            get { return GetProperty(() => AssemblyIncluded); }
            set { SetProperty(() => AssemblyIncluded, value); }
        }

        public bool QuantityCanBeChanged
        {
            get
            {
                bool canBeChanged;

                if (ParentOrder?.Bonuses?.Any(x => x.OrderProductId == Id) == true)
                {
                    return false;
                }

                if (ParentOrder != null && ParentRecordId == null)
                {
                    canBeChanged = ParentOrder.GetGiftOrderProducts(Id).All(x => OrderRules.IsProductQuantityCanBeChanged(x))
                    && ParentOrder.GetAdditionalServiceOrderProductsAndAdditionalServiceConsumableProducts(Id).All(x => OrderRules.IsProductQuantityCanBeChanged(x))
                    && ParentOrder.GetChildOrderProducts(Id).All(x => OrderRules.IsProductQuantityCanBeChanged(x));
                }
                else
                {
                    canBeChanged = false;
                }

                canBeChanged = canBeChanged
                       && OrderRules.IsProductQuantityCanBeChanged(this)
                       && ParentOrder.IsLockedByCurrentUserAndEditingAllowed
                       && (!ParentOrder.ExternalPayments.Any()
                           || ParentOrder.ExternalPayments.All(x => Payment.IsEditingAllowed(x.Payment.Id, x.PaymentStateId, x.Payment.Credit, x.Payment.PartialCredit)))
                       && ParentId == null
                       && Product.TypeId != ProductType.GuestProductId;

                if (IsVirtualProduct && ParentOrder != null && ParentOrder.OrderProducts.Where(x => x.OrderFolderId == OrderFolderId).Any(x => x.Source.Id != OrderProductSourceType.NoneId))
                {
                    canBeChanged = false;
                }

                return canBeChanged;
            }
        }

        public string Sn
        {
            get { return GetProperty(() => Sn); }
            set { SetProperty(() => Sn, value); }
        }

        public OrderProductSource Source
        {
            get { return GetProperty(() => Source); }
            set { SetProperty(() => Source, value, SourceChangedCallback); }
        }

        public int? OrderWarehouseId
        {
            get { return GetProperty(() => OrderWarehouseId); }
            set { SetProperty(() => OrderWarehouseId, value, () => RaisePropertyChanged(nameof(IsOnWarehouse))); }
        }

        public int WarrantyId
        {
            get { return GetProperty(() => WarrantyId); }
            set { SetProperty(() => WarrantyId, value); }
        }

        public OrderProductStatus State
        {
            get { return GetProperty(() => State); }
            set { SetProperty(() => State, value, StateChangedCallback); }
        }

        public bool IsVirtualProduct
        {
            get { return GetProperty(() => IsVirtualProduct); }
            set { SetProperty(() => IsVirtualProduct, value); }
        }

        public int? BonusTypeId
        {
            get { return GetProperty(() => BonusTypeId); }
            set { SetProperty(() => BonusTypeId, value); }
        }

        public int TypeId
        {
            get { return GetProperty(() => TypeId); }
            set { SetProperty(() => TypeId, value); }
        }

        public bool AdditionalWarranty
        {
            get { return GetProperty(() => AdditionalWarranty); }
            set { SetProperty(() => AdditionalWarranty, value); }
        }

        public int? BonusesToCharge
        {
            get { return GetProperty(() => BonusesToCharge); }
            set { SetProperty(() => BonusesToCharge, value); }
        }

        public bool BonusesCharged
        {
            get { return GetProperty(() => BonusesCharged); }
            set { SetProperty(() => BonusesCharged, value); }
        }

        public int? MaxBonusesToUse
        {
            get { return GetProperty(() => MaxBonusesToUse); }
            set { SetProperty(() => MaxBonusesToUse, value); }
        }

        public OrderViewModel ParentOrder { get; set; }

        public int? AssembliesInAssemblyModule { get; set; }

        public int? CompletedAdditionalServiceProductsQuantity { get; set; }

        public bool FreeDelivery { get; set; }

        public bool IsGuestProduct => Product?.TypeId == ProductType.GuestProductId;

        public bool IsOnWarehouse => State == OrderProductStatus.Agreed && Source is WarehouseOrderProductSource && OrderWarehouseId == Source.WarehouseId;

        public string Error => string.Empty;

        Price IOrderPaymentInfoProduct.Price => new Price(PriceOut, CurrencyOutId);

        Price IOrderPaymentInfoProduct.OriginalPrice => new Price(Price, CurrencyId);

        private IOrderRules OrderRules { get; }

        private IMessageFacadeService MessageFacadeService { get; }

        private IDictionaries Dictionaries { get; }

        public string this[string columnName] => IDataErrorInfoHelper.GetErrorText(this, columnName);

        public static void BuildMetadata(MetadataBuilder<OrderProductViewModel> builder)
        {
            builder.Property(x => x.Quantity)
                .MatchesRule(x => x > 0, () => Resources.ValueOutOfRangeErrorMessage)
                .MatchesInstanceRule((x, y) => y.AssembliesInAssemblyModule == null || x >= y.AssembliesInAssemblyModule || !y.IsVirtualProduct || y.AssembliesInAssemblyModule > 0, () => "Сборок не должно быть меньше чем в модуле \"Сборка\"")
                .MatchesInstanceRule((x, y) => y.CompletedAdditionalServiceProductsQuantity == null || x >= y.CompletedAdditionalServiceProductsQuantity, () => "Услуг не должно быть меньше, чем услуг привязанных к товару, в статусе \"Завершена\"");

            builder.Property(x => x.PriceOut)
                .MatchesInstanceRule((x, y) => (x is > 0 and <= Constants.MaxProductPrice) || (x == 0 && y.TypeId == ProductType.GuestProductId), () => Resources.ValueOutOfRangeErrorMessage)
                .MatchesInstanceRule((x, y) => y.ParentOrder?.SelectedPayment?.Id != Payment.CashlessTaxId || y.InvoicePrice == null || x >= y.InvoicePrice, (_, y) => $"Цена товара не должна быть меньше чем цена товара в накладной {y.InvoicePrice} грн.");

            builder.Property(x => x.PriceId)
                .MatchesInstanceRule((x, y) => x is null || y.Dictionaries is null || y.Dictionaries.GetItemById<ProductPriceKind>(x.Value).CanSwitchInOrders, () => "Колонка цен не доступна для выбора в заказе");
        }

        void IDXDataErrorInfo.GetError(ErrorInfo info)
        {
        }

        void IDXDataErrorInfo.GetPropertyError(string propertyName, ErrorInfo info)
        {
        }

        public void RefreshAllowEdit()
        {
            RaisePropertiesChanged(nameof(PriceCanBeChanged), nameof(QuantityCanBeChanged));
        }

        public void RaiseProperties(params string[] propertyNames)
        {
            RaisePropertiesChanged(propertyNames);
        }

        public bool IsAssembly()
        {
            return OrderFolderId.HasValue && OrderFolder != null && (OrderFolder.TypeId == OrderFolderType.AssemblyServiceId || OrderFolder.TypeId == OrderFolderType.AssembledComputerRuleId || OrderFolder.TypeId == OrderFolderType.DisassemblyService.Id);
        }

        public bool IsAssemblyOrAssembledComputerRule()
        {
            return OrderFolderId.HasValue && OrderFolder != null && (OrderFolder.TypeId == OrderFolderType.AssemblyServiceId || OrderFolder.TypeId == OrderFolderType.AssembledComputerRuleId || OrderFolder.TypeId == OrderFolderType.DisassemblyService.Id);
        }

        protected override void OnParentViewModelChanged(object parentViewModel)
        {
            base.OnParentViewModelChanged(parentViewModel);

            ParentOrder = (OrderViewModel)parentViewModel;
        }

        private void CurrencyOutIdChangedCallback()
        {
            if (ParentOrder != null)
            {
                ParentOrder.OrderProductsChanged();
                ParentOrder.RefreshPaymentInfo();

                foreach (OrderProductViewModel giftOrderProduct in ParentOrder.GetGiftOrderProducts(Id))
                {
                    giftOrderProduct.CurrencyOutId = CurrencyOutId;
                }
            }

            ValidationStarted?.Invoke();
        }

        private void PriceIdChanged()
        {
            if (ParentOrder?.Loaded != true || PriceId is null)
            {
                return;
            }

            if (ParentRecordId.HasValue && ParentOrder.OrderProducts.First(x => x.Id == ParentRecordId.Value).OrderFolder?.TypeId == OrderFolderType.AssembledComputerRuleId)
            {
                PriceId = null;
                PriceIdOld = null;
            }
            else
            {
                ProductPriceSimpleDto productPrice = Product.Prices.FirstOrDefault(x => x.PriceTypeId == PriceId.Value);

                if (productPrice is null || productPrice.CurrencyId == 0)
                {
                    MessageFacadeService.ShowNotificationError("Для товара не заполнен выбранный тип цены.");
                    PriceId = PriceIdOld;
                    return;
                }

                if (CurrencyOutId != productPrice.CurrencyId)
                {
                    MessageFacadeService.ShowNotificationError("У товара в выбранной цене не соответствует \nвалюта.");
                    PriceId = PriceIdOld;
                    return;
                }

                PriceOut = productPrice.Price;
                MaxBonusesToUse = productPrice.MaxBonusesToUse;
                PriceIdOld = PriceId;

                OrderProductViewModel additionalService = ParentOrder.OrderProducts.FirstOrDefault(x => x.IsAdditionalService && Id == x.ParentRecordId);

                if (additionalService != null && !additionalService.IsGift)
                {
                    if (additionalService.AdditionalServicePercent is null || additionalService.AdditionalServiceMinPrice is null)
                    {
                        MessageFacadeService.ShowNotificationError("Недостаточно данных для пересчета услуги");
                        return;
                    }

                    additionalService.PriceOut = PriceHelper.CalculatePriceForAdditionalService(PriceOut, additionalService.AdditionalServicePercent.Value, additionalService.AdditionalServiceMinPrice.Value);
                }

                if (PriceId == ProductPriceKind.Telemart1)
                {
                    BonusesToCharge ??= Product.BonusesToCharge;
                }
                else
                {
                    BonusesToCharge = null;
                }
            }
        }

        private void QuantityChangedCallback(int oldQuantity)
        {
            if (ParentOrder != null)
            {
                RaisePropertiesChanged(nameof(AssembliesInAssemblyModule), nameof(CompletedAdditionalServiceProductsQuantity));
                ParentOrder.OrderProductsChanged();
                ParentOrder.RefreshPaymentInfo();

                foreach (OrderProductViewModel giftOrderProduct in ParentOrder.GetGiftOrderProducts(Id))
                {
                    giftOrderProduct.Quantity = Quantity;
                }

                foreach (OrderProductViewModel additionalServiceOrderProduct in ParentOrder.GetAdditionalServiceOrderProductsAndAdditionalServiceConsumableProducts(Id))
                {
                    additionalServiceOrderProduct.Quantity = Quantity;
                }

                ParentOrder.RefreshSummaryItems();

                ValidationStarted?.Invoke();

                if (IsVirtualProduct)
                {
                    foreach (OrderFolderDto orderFolder in ParentOrder.OrderProducts.Where(x => x.OrderFolderId == OrderFolderId).Select(x => x.OrderFolder))
                    {
                        orderFolder.Quantity = Quantity;
                    }

                    if (Quantity < oldQuantity)
                    {
                        DeleteAssemblyProducts(oldQuantity);
                    }
                    else
                    {
                        AddAssemblyProducts(oldQuantity);
                    }
                }
            }
        }

        private void AssemblyQuantityChangedCallback(int? oldAssemblyQuantity)
        {
            if (ParentOrder != null)
            {
                foreach (OrderProductViewModel giftOrderProduct in ParentOrder.GetGiftOrderProducts(Id))
                {
                    giftOrderProduct.AssemblyQuantity = AssemblyQuantity;
                }

                foreach (OrderProductViewModel additionalServiceOrderProduct in ParentOrder.GetAdditionalServiceOrderProductsAndAdditionalServiceConsumableProducts(Id))
                {
                    additionalServiceOrderProduct.AssemblyQuantity = AssemblyQuantity;
                }
            }
        }

        private void DeleteAssemblyProducts(int oldQuantity)
        {
            List<OrderProductViewModel> itemsToDelete = new List<OrderProductViewModel>();

            IEnumerable<IGrouping<int, OrderProductViewModel>> productsGroup = ParentOrder.OrderProducts
                .Where(x => !x.IsGift && !x.IsAdditionalService && !x.IsVirtualProduct && x.OrderFolderId == OrderFolderId)
                .GroupBy(x => x.Product.Id);

            foreach (IGrouping<int, OrderProductViewModel> group in productsGroup)
            {
                int assemblyQuantity = group.First().AssemblyQuantity!.Value;

                int removeCount = (oldQuantity * assemblyQuantity) - (Quantity * assemblyQuantity);

                foreach (OrderProductViewModel product in group)
                {
                    if (product.Quantity <= removeCount)
                    {
                        itemsToDelete.Add(product);
                        itemsToDelete.AddRange(ParentOrder.OrderProducts.Where(x => x.ParentRecordId == product.Id));
                        removeCount -= product.Quantity;
                    }
                    else
                    {
                        product.Quantity -= removeCount;
                        break;
                    }
                }
            }

            ParentOrder.OrderProducts.RemoveRange(itemsToDelete);
        }

        private void AddAssemblyProducts(int oldQuantity)
        {
            IEnumerable<IGrouping<int, OrderProductViewModel>> productsGroup = ParentOrder.OrderProducts
                .Where(x => !x.IsGift && !x.IsAdditionalService && !x.IsVirtualProduct && x.OrderFolderId == OrderFolderId)
                .GroupBy(x => x.Product.Id);

            foreach (IGrouping<int, OrderProductViewModel> group in productsGroup)
            {
                int assemblyQuantity = group.First().AssemblyQuantity!.Value;

                int addCount = (Quantity * assemblyQuantity) - (oldQuantity * assemblyQuantity);

                group.First().Quantity += addCount;
            }
        }

        private void PriceChangedCallback()
        {
            if (ParentOrder != null)
            {
                ParentOrder.OrderProductsChanged();
                ParentOrder.RefreshPaymentInfo();

                if (OrderFolderId.HasValue)
                {
                    decimal price = ParentOrder.OrderProducts
                        .Where(x => x.OrderFolderId == OrderFolderId && !x.IsVirtualProduct)
                        .GroupBy(x => x.Product.Id)
                        .Sum(x => x.Select(y => y.PriceOut * y.AssemblyQuantity!.Value).First());

                    ParentOrder.OrderProducts
                        .Where(x => x.IsVirtualProduct && x.OrderFolderId == OrderFolderId)
                        .ForEach(x => x.PriceOut = price);
                }

                if (ParentOrder.Loaded && Product?.Prices?.Any() == true)
                {
                    if (ParentRecordId.HasValue && ParentOrder.OrderProducts.First(x => x.Id == ParentRecordId.Value).OrderFolder?.TypeId == OrderFolderType.AssembledComputerRuleId)
                    {
                        PriceId = null;
                        PriceIdOld = null;
                    }
                    else
                    {
                        if (PriceId.HasValue)
                        {
                            ProductPriceSimpleDto productPrice = Product.Prices.FirstOrDefault(x => x.PriceTypeId == PriceId.Value);

                            if (productPrice != null && PriceOut + PromoDiscount != productPrice.Price)
                            {
                                PriceId = null;
                                PriceIdOld = null;
                            }
                        }

                        ProductPriceKind[] priceKinds = Dictionaries
                            .GetItems<ProductPriceKind>()
                            .Where(x => x.PriceColumn != null)
                            .OrderBy(x => x.PriceColumn)
                            .ToArray();

                        foreach (ProductPriceKind priceKind in priceKinds)
                        {
                            ProductPriceSimpleDto productPrice = Product.Prices.FirstOrDefault(x => x.PriceTypeId == priceKind.Id);

                            if (productPrice != null && CurrencyOutId == productPrice.CurrencyId && PriceOut == productPrice.Price)
                            {
                                PriceId = priceKind.Id;
                                PriceIdOld = priceKind.Id;
                                MaxBonusesToUse = productPrice.MaxBonusesToUse;
                                break;
                            }
                        }
                    }
                }
            }

            ValidationStarted?.Invoke();
        }

        private void SourceChangedCallback()
        {
            RaisePropertiesChanged(
                nameof(PriceCanBeChanged),
                nameof(QuantityCanBeChanged),
                nameof(IsOnWarehouse));

            ParentOrder?.OrderProductsChanged();
            ValidationStarted?.Invoke();
        }

        private void StateChangedCallback()
        {
            RaisePropertiesChanged(
                nameof(PriceCanBeChanged),
                nameof(QuantityCanBeChanged),
                nameof(IsOnWarehouse));

            if (ParentOrder != null)
            {
                ParentOrder.RefreshPaymentInfo();

                foreach (OrderProductViewModel orderProduct in ParentOrder.OrderProducts)
                {
                    orderProduct.RaisePropertyChanged(nameof(QuantityCanBeChanged));
                }
            }

            ValidationStarted?.Invoke();
        }
    }
}