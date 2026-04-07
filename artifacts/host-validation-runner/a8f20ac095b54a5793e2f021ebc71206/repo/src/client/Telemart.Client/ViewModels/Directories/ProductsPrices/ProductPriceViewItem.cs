using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Common;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects.Prices;
using Telemart.Client.TransferObjects.Promo;
using Telemart.Common.Localization;
using Telemart.Common.PriceConversion;
using Telemart.Common.TransferObjects;
using Telemart.PriceCalculation;
using Telemart.PriceCalculation.Context;
using ProductLabel = Telemart.Client.Dictionaries.ProductLabel;
using TagColor = Telemart.Client.Dictionaries.TagColor;

namespace Telemart.Client.ViewModels.Directories.ProductsPrices
{
    public sealed class ProductPriceViewItem : BindableBase, IDataErrorInfo, ILocalіzableEntity
    {
        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public string Name
        {
            get { return GetProperty(() => Name); }
            set { SetProperty(() => Name, value, () => RaisePropertyChanged(nameof(DisplayName))); }
        }

        public string NameUkr
        {
            get { return GetProperty(() => NameUkr); }
            set { SetProperty(() => NameUkr, value, () => RaisePropertyChanged(nameof(DisplayName))); }
        }

        public string NameEn
        {
            get { return GetProperty(() => NameEn); }
            set { SetProperty(() => NameEn, value, () => RaisePropertyChanged(nameof(DisplayName))); }
        }

        public double Active
        {
            get { return GetProperty(() => Active); }
            set { SetProperty(() => Active, value); }
        }

        public string Pn
        {
            get { return GetProperty(() => Pn); }
            set { SetProperty(() => Pn, value); }
        }

        public int? ProductTypeId
        {
            get { return GetProperty(() => ProductTypeId); }
            set { SetProperty(() => ProductTypeId, value); }
        }

        public bool Preorder
        {
            get { return GetProperty(() => Preorder); }
            set { SetProperty(() => Preorder, value); }
        }

        public ProductAvailability Avail
        {
            get
            {
                return GetProperty(() => Avail);
            }

            set
            {
                SetProperty(
                    () => Avail,
                    value,
                    () =>
                    {
                        RaisePropertiesChanged(nameof(IsChanged), nameof(AvailColor));
                        RaisePricePropertiesChanged();
                    });
            }
        }

        public ProductAvailability AvailOld
        {
            get { return GetProperty(() => AvailOld); }
            set { SetProperty(() => AvailOld, value, () => { RaisePropertiesChanged(nameof(IsChanged), nameof(AvailColor)); }); }
        }

        public ProductLabel LabelRetail
        {
            get { return GetProperty(() => LabelRetail); }
            set { SetProperty(() => LabelRetail, value, () => { RaisePropertiesChanged(nameof(IsChanged)); }); }
        }

        public ProductLabel LabelRetailOld
        {
            get { return GetProperty(() => LabelRetailOld); }
            set { SetProperty(() => LabelRetailOld, value, () => { RaisePropertiesChanged(nameof(IsChanged)); }); }
        }

        public int? AvailModifiedBy
        {
            get { return GetProperty(() => AvailModifiedBy); }
            set { SetProperty(() => AvailModifiedBy, value); }
        }

        public ProductLabel LabelWholesale
        {
            get { return GetProperty(() => LabelWholesale); }
            set { SetProperty(() => LabelWholesale, value, () => { RaisePropertiesChanged(nameof(IsChanged)); }); }
        }

        public ProductLabel LabelWholesaleOld
        {
            get { return GetProperty(() => LabelWholesaleOld); }
            set { SetProperty(() => LabelWholesaleOld, value, () => { RaisePropertiesChanged(nameof(IsChanged)); }); }
        }

        public decimal? PriceInUsd
        {
            get { return GetProperty(() => PriceInUsd); }
            set { SetProperty(() => PriceInUsd, value); }
        }

        public decimal? PriceCompetitor
        {
            get { return GetProperty(() => PriceCompetitor); }
            set { SetProperty(() => PriceCompetitor, value); }
        }

        public decimal? PriceTransit
        {
            get { return GetProperty(() => PriceTransit); }
            set { SetProperty(() => PriceTransit, value); }
        }

        public DateTime CreatedOn
        {
            get { return GetProperty(() => CreatedOn); }
            set { SetProperty(() => CreatedOn, value); }
        }

        public int Visits
        {
            get { return GetProperty(() => Visits); }
            set { SetProperty(() => Visits, value); }
        }

        public int InStock
        {
            get { return GetProperty(() => InStock); }
            set { SetProperty(() => InStock, value); }
        }

        public int StorageInStock
        {
            get { return GetProperty(() => StorageInStock); }
            set { SetProperty(() => StorageInStock, value); }
        }

        public int TransitInStock
        {
            get { return GetProperty(() => TransitInStock); }
            set { SetProperty(() => TransitInStock, value); }
        }

        public int ShowcasesStock
        {
            get { return GetProperty(() => ShowcasesStock); }
            set { SetProperty(() => ShowcasesStock, value); }
        }

        public int MainStock
        {
            get { return GetProperty(() => MainStock); }
            set { SetProperty(() => MainStock, value); }
        }

        public int ShowcasesCapacity
        {
            get { return GetProperty(() => ShowcasesCapacity); }
            set { SetProperty(() => ShowcasesCapacity, value); }
        }

        public int? DaysInStock
        {
            get { return GetProperty(() => DaysInStock); }
            set { SetProperty(() => DaysInStock, value); }
        }

        public int ReservedQuantity
        {
            get { return GetProperty(() => ReservedQuantity); }
            set { SetProperty(() => ReservedQuantity, value, () => { RaisePropertiesChanged(nameof(IsChanged)); }); }
        }

        public int ReservedQuantityOld
        {
            get { return GetProperty(() => ReservedQuantityOld); }
            set { SetProperty(() => ReservedQuantityOld, value, () => { RaisePropertiesChanged(nameof(IsChanged)); }); }
        }

        public double Density14Days
        {
            get { return GetProperty(() => Density14Days); }
            set { SetProperty(() => Density14Days, value); }
        }

        public double Density1Month
        {
            get { return GetProperty(() => Density1Month); }
            set { SetProperty(() => Density1Month, value); }
        }

        public int Position
        {
            get { return GetProperty(() => Position); }
            set { SetProperty(() => Position, value); }
        }

        public string SegmentName
        {
            get { return GetProperty(() => SegmentName); }
            set { SetProperty(() => SegmentName, value); }
        }

        public string SegmentToolTip
        {
            get { return GetProperty(() => SegmentToolTip); }
            set { SetProperty(() => SegmentToolTip, value); }
        }

        public int? SegmentPriceOutAbcId
        {
            get { return GetProperty(() => SegmentPriceOutAbcId); }
            set { SetProperty(() => SegmentPriceOutAbcId, value); }
        }

        public int? SegmentProfitAbcId
        {
            get { return GetProperty(() => SegmentProfitAbcId); }
            set { SetProperty(() => SegmentProfitAbcId, value); }
        }

        public int? SegmentOrderQuantityAbcId
        {
            get { return GetProperty(() => SegmentOrderQuantityAbcId); }
            set { SetProperty(() => SegmentOrderQuantityAbcId, value); }
        }

        public int? ProductCategoryPriceOutAbcId
        {
            get { return GetProperty(() => ProductCategoryPriceOutAbcId); }
            set { SetProperty(() => ProductCategoryPriceOutAbcId, value); }
        }

        public int? ProductCategoryProfitAbcId
        {
            get { return GetProperty(() => ProductCategoryProfitAbcId); }
            set { SetProperty(() => ProductCategoryProfitAbcId, value); }
        }

        public int? ProductCategoryOrderQuantityAbcId
        {
            get { return GetProperty(() => ProductCategoryOrderQuantityAbcId); }
            set { SetProperty(() => ProductCategoryOrderQuantityAbcId, value); }
        }

        public int? SegmentCategoryPriceOutAbcId
        {
            get { return GetProperty(() => SegmentCategoryPriceOutAbcId); }
            set { SetProperty(() => SegmentCategoryPriceOutAbcId, value); }
        }

        public int? SegmentCategoryProfitAbcId
        {
            get { return GetProperty(() => SegmentCategoryProfitAbcId); }
            set { SetProperty(() => SegmentCategoryProfitAbcId, value); }
        }

        public int? SegmentCategoryOrderQuantityAbcId
        {
            get { return GetProperty(() => SegmentCategoryOrderQuantityAbcId); }
            set { SetProperty(() => SegmentCategoryOrderQuantityAbcId, value); }
        }

        public int SalesCurrentMonth
        {
            get { return GetProperty(() => SalesCurrentMonth); }
            set { SetProperty(() => SalesCurrentMonth, value); }
        }

        public int SalesLastMonth
        {
            get { return GetProperty(() => SalesLastMonth); }
            set { SetProperty(() => SalesLastMonth, value); }
        }

        public int SalesBeforeLastMonth
        {
            get { return GetProperty(() => SalesBeforeLastMonth); }
            set { SetProperty(() => SalesBeforeLastMonth, value); }
        }

        public int OrdersCurrentMonth
        {
            get { return GetProperty(() => OrdersCurrentMonth); }
            set { SetProperty(() => OrdersCurrentMonth, value); }
        }

        public int OrdersLastMonth
        {
            get { return GetProperty(() => OrdersLastMonth); }
            set { SetProperty(() => OrdersLastMonth, value); }
        }

        public int OrdersBeforeLastMonth
        {
            get { return GetProperty(() => OrdersBeforeLastMonth); }
            set { SetProperty(() => OrdersBeforeLastMonth, value); }
        }

        public decimal LastSalePriceUsd
        {
            get { return GetProperty(() => LastSalePriceUsd); }
            set { SetProperty(() => LastSalePriceUsd, value); }
        }

        public double Conversion
        {
            get { return GetProperty(() => Conversion); }
            set { SetProperty(() => Conversion, value); }
        }

        public int DaysFromLastSale
        {
            get { return GetProperty(() => DaysFromLastSale); }
            set { SetProperty(() => DaysFromLastSale, value); }
        }

        public int ProductCurrencyId
        {
            get { return GetProperty(() => ProductCurrencyId); }
            set { SetProperty(() => ProductCurrencyId, value); }
        }

        public int? HotlinePosition
        {
            get { return GetProperty(() => HotlinePosition); }
            set { SetProperty(() => HotlinePosition, value); }
        }

        public decimal? HotlineMinPriceUsd
        {
            get { return GetProperty(() => HotlineMinPriceUsd); }
            set { SetProperty(() => HotlineMinPriceUsd, value); }
        }

        public decimal? HotlinePriceUsd
        {
            get { return GetProperty(() => HotlinePriceUsd); }
            set { SetProperty(() => HotlinePriceUsd, value); }
        }

        public ProductPriceDataViewItem Telemart1PriceData
        {
            get { return GetProperty(() => Telemart1PriceData); }
            set { SetProperty(() => Telemart1PriceData, value); }
        }

        public ProductPriceDataViewItem Telemart2PriceData
        {
            get { return GetProperty(() => Telemart2PriceData); }
            set { SetProperty(() => Telemart2PriceData, value); }
        }

        public ProductPriceDataViewItem Telemart3PriceData
        {
            get { return GetProperty(() => Telemart3PriceData); }
            set { SetProperty(() => Telemart3PriceData, value); }
        }

        public ProductPriceDataViewItem Telemart4PriceData
        {
            get { return GetProperty(() => Telemart4PriceData); }
            set { SetProperty(() => Telemart4PriceData, value); }
        }

        public ProductPriceDataViewItem Telemart5PriceData
        {
            get { return GetProperty(() => Telemart5PriceData); }
            set { SetProperty(() => Telemart5PriceData, value); }
        }

        public ProductPriceDataViewItem Configurator1PriceData
        {
            get { return GetProperty(() => Configurator1PriceData); }
            set { SetProperty(() => Configurator1PriceData, value); }
        }

        public ProductPriceDataViewItem Configurator2PriceData
        {
            get { return GetProperty(() => Configurator2PriceData); }
            set { SetProperty(() => Configurator2PriceData, value); }
        }

        public ProductPriceDataViewItem Configurator3PriceData
        {
            get { return GetProperty(() => Configurator3PriceData); }
            set { SetProperty(() => Configurator3PriceData, value); }
        }

        public ProductPriceDataViewItem Configurator4PriceData
        {
            get { return GetProperty(() => Configurator4PriceData); }
            set { SetProperty(() => Configurator4PriceData, value); }
        }

        public ProductPriceDataViewItem Configurator5PriceData
        {
            get { return GetProperty(() => Configurator5PriceData); }
            set { SetProperty(() => Configurator5PriceData, value); }
        }

        public ProductPriceDataViewItem Configurator6PriceData
        {
            get { return GetProperty(() => Configurator6PriceData); }
            set { SetProperty(() => Configurator6PriceData, value); }
        }

        public ProductPriceDataViewItem Configurator7PriceData
        {
            get { return GetProperty(() => Configurator7PriceData); }
            set { SetProperty(() => Configurator7PriceData, value); }
        }

        public ProductPriceDataViewItem Configurator8PriceData
        {
            get { return GetProperty(() => Configurator8PriceData); }
            set { SetProperty(() => Configurator8PriceData, value); }
        }

        public ProductPriceDataViewItem Configurator9PriceData
        {
            get { return GetProperty(() => Configurator9PriceData); }
            set { SetProperty(() => Configurator9PriceData, value); }
        }

        public ProductPriceDataViewItem Configurator10PriceData
        {
            get { return GetProperty(() => Configurator10PriceData); }
            set { SetProperty(() => Configurator10PriceData, value); }
        }

        public ProductPriceDataViewItem WarehousePriceData
        {
            get { return GetProperty(() => WarehousePriceData); }
            set { SetProperty(() => WarehousePriceData, value); }
        }

        public ProductPriceDataViewItem PriceInPriceData
        {
            get { return GetProperty(() => PriceInPriceData); }
            set { SetProperty(() => PriceInPriceData, value); }
        }

        public int Hotline
        {
            get { return GetProperty(() => Hotline); }
            set { SetProperty(() => Hotline, value, () => { RaisePropertiesChanged(nameof(IsChanged)); }); }
        }

        public int HotlineOld
        {
            get { return GetProperty(() => HotlineOld); }
            set { SetProperty(() => HotlineOld, value, () => { RaisePropertiesChanged(nameof(IsChanged)); }); }
        }

        public int Ratio
        {
            get { return GetProperty(() => Ratio); }
            set { SetProperty(() => Ratio, value, () => { RaisePropertiesChanged(nameof(IsChanged)); }); }
        }

        public int RatioOld
        {
            get { return GetProperty(() => RatioOld); }
            set { SetProperty(() => RatioOld, value, () => { RaisePropertiesChanged(nameof(IsChanged)); }); }
        }

        public int? MinLeftover
        {
            get { return GetProperty(() => MinLeftover); }
            set { SetProperty(() => MinLeftover, value, () => { RaisePropertiesChanged(nameof(IsChanged)); }); }
        }

        public int? MinLeftoverOld
        {
            get { return GetProperty(() => MinLeftoverOld); }
            set { SetProperty(() => MinLeftoverOld, value, () => { RaisePropertiesChanged(nameof(IsChanged)); }); }
        }

        public int Rozetka
        {
            get { return GetProperty(() => Rozetka); }
            set { SetProperty(() => Rozetka, value, () => { RaisePropertiesChanged(nameof(IsChanged)); }); }
        }

        public int RozetkaOld
        {
            get { return GetProperty(() => RozetkaOld); }
            set { SetProperty(() => RozetkaOld, value, () => { RaisePropertiesChanged(nameof(IsChanged)); }); }
        }

        public int Monomarket
        {
            get { return GetProperty(() => Monomarket); }
            set { SetProperty(() => Monomarket, value, () => { RaisePropertiesChanged(nameof(IsChanged)); }); }
        }

        public int MonomarketOld
        {
            get { return GetProperty(() => MonomarketOld); }
            set { SetProperty(() => MonomarketOld, value, () => { RaisePropertiesChanged(nameof(IsChanged)); }); }
        }

        public bool Mining
        {
            get { return GetProperty(() => Mining); }
            set { SetProperty(() => Mining, value, () => { RaisePropertiesChanged(nameof(IsChanged)); }); }
        }

        public bool MiningOld
        {
            get { return GetProperty(() => MiningOld); }
            set { SetProperty(() => MiningOld, value, () => { RaisePropertiesChanged(nameof(IsChanged)); }); }
        }

        public int? ShowcasePickupModeId
        {
            get { return GetProperty(() => ShowcasePickupModeId); }
            set { SetProperty(() => ShowcasePickupModeId, value, () => { RaisePropertiesChanged(nameof(IsChanged)); }); }
        }

        public int? ShowcasePickupModeIdOld
        {
            get { return GetProperty(() => ShowcasePickupModeIdOld); }
            set { SetProperty(() => ShowcasePickupModeIdOld, value, () => { RaisePropertiesChanged(nameof(IsChanged)); }); }
        }

        public bool FreeDelivery
        {
            get { return GetProperty(() => FreeDelivery); }
            set { SetProperty(() => FreeDelivery, value, () => { RaisePropertiesChanged(nameof(IsChanged)); }); }
        }

        public bool FreeDeliveryOld
        {
            get { return GetProperty(() => FreeDeliveryOld); }
            set { SetProperty(() => FreeDeliveryOld, value, () => { RaisePropertiesChanged(nameof(IsChanged)); }); }
        }

        public double F2Markup
        {
            get { return GetProperty(() => F2Markup); }
            set { SetProperty(() => F2Markup, value, () => { RaisePropertiesChanged(nameof(IsChanged)); }); }
        }

        public decimal MinProductMarginPercent
        {
            get { return GetProperty(() => MinProductMarginPercent); }
            set { SetProperty(() => MinProductMarginPercent, value, () => { RaisePropertiesChanged(nameof(IsChanged)); }); }
        }

        public decimal? MinCategoryMarginPercent
        {
            get { return GetProperty(() => MinCategoryMarginPercent); }
            set { SetProperty(() => MinCategoryMarginPercent, value, () => { RaisePropertiesChanged(nameof(IsChanged)); }); }
        }

        public double PlannedMarkup
        {
            get { return GetProperty(() => PlannedMarkup); }
            set { SetProperty(() => PlannedMarkup, value); }
        }

        public bool UsePlannedMarkupInAutoShowcase
        {
            get { return GetProperty(() => UsePlannedMarkupInAutoShowcase); }
            set { SetProperty(() => UsePlannedMarkupInAutoShowcase, value); }
        }

        public bool ShowInAccessories
        {
            get { return GetProperty(() => ShowInAccessories); }
            set { SetProperty(() => ShowInAccessories, value, () => { RaisePropertiesChanged(nameof(IsChanged)); }); }
        }

        public bool ShowInAccessoriesOld
        {
            get { return GetProperty(() => ShowInAccessoriesOld); }
            set { SetProperty(() => ShowInAccessoriesOld, value, () => { RaisePropertiesChanged(nameof(IsChanged)); }); }
        }

        public int? BonusTypeIdOld
        {
            get { return GetProperty(() => BonusTypeIdOld); }
            set { SetProperty(() => BonusTypeIdOld, value, () => { RaisePropertiesChanged(nameof(IsChanged)); }); }
        }

        public int? BonusesToChargeOld
        {
            get { return GetProperty(() => BonusesToChargeOld); }
            set { SetProperty(() => BonusesToChargeOld, value, () => { RaisePropertiesChanged(nameof(IsChanged)); }); }
        }

        public int PlannedLeftover
        {
            get { return GetProperty(() => PlannedLeftover); }
            set { SetProperty(() => PlannedLeftover, value); }
        }

        public ProductPriceDataViewItem ParentPrice1
        {
            get { return GetProperty(() => ParentPrice1); }
            set { SetProperty(() => ParentPrice1, value); }
        }

        public ProductPriceDataViewItem ParentPrice2
        {
            get { return GetProperty(() => ParentPrice2); }
            set { SetProperty(() => ParentPrice2, value); }
        }

        public ProductPriceDataViewItem ParentPrice3
        {
            get { return GetProperty(() => ParentPrice3); }
            set { SetProperty(() => ParentPrice3, value); }
        }

        public ProductPriceDataViewItem ParentPrice4
        {
            get { return GetProperty(() => ParentPrice4); }
            set { SetProperty(() => ParentPrice4, value); }
        }

        public ProductPriceDataViewItem ParentPrice5
        {
            get { return GetProperty(() => ParentPrice5); }
            set { SetProperty(() => ParentPrice5, value); }
        }

        public ProductPriceDataViewItem AssembledComputerRuleBasePrice1
        {
            get { return GetProperty(() => AssembledComputerRuleBasePrice1); }
            set { SetProperty(() => AssembledComputerRuleBasePrice1, value); }
        }

        public ProductPriceDataViewItem AssembledComputerRuleBasePrice2
        {
            get { return GetProperty(() => AssembledComputerRuleBasePrice2); }
            set { SetProperty(() => AssembledComputerRuleBasePrice2, value); }
        }

        public ProductPriceDataViewItem AssembledComputerRuleBasePrice3
        {
            get { return GetProperty(() => AssembledComputerRuleBasePrice3); }
            set { SetProperty(() => AssembledComputerRuleBasePrice3, value); }
        }

        public ProductPriceDataViewItem AssembledComputerRuleBasePrice4
        {
            get { return GetProperty(() => AssembledComputerRuleBasePrice4); }
            set { SetProperty(() => AssembledComputerRuleBasePrice4, value); }
        }

        public ProductPriceDataViewItem AssembledComputerRuleBasePrice5
        {
            get { return GetProperty(() => AssembledComputerRuleBasePrice5); }
            set { SetProperty(() => AssembledComputerRuleBasePrice5, value); }
        }

        public double WarrantyRetail
        {
            get { return GetProperty(() => WarrantyRetail); }
            set { SetProperty(() => WarrantyRetail, value); }
        }

        public double? OldMaxTradeInPrice
        {
            get { return GetProperty(() => OldMaxTradeInPrice); }
            set { SetProperty(() => OldMaxTradeInPrice, value, () => { RaisePropertiesChanged(nameof(IsChanged)); }); }
        }

        public double? MaxTradeInPrice
        {
            get { return GetProperty(() => MaxTradeInPrice); }
            set { SetProperty(() => MaxTradeInPrice, value, () => { RaisePropertiesChanged(nameof(IsChanged)); }); }
        }

        public string PriceComment
        {
            get { return GetProperty(() => PriceComment); }
            set { SetProperty(() => PriceComment, value, () => { RaisePropertiesChanged(nameof(IsChanged)); }); }
        }

        public string PriceCommentOld
        {
            get { return GetProperty(() => PriceCommentOld); }
            set { SetProperty(() => PriceCommentOld, value, () => { RaisePropertiesChanged(nameof(IsChanged)); }); }
        }

        public PriceRobotMode RobotModeManual
        {
            get { return GetProperty(() => RobotModeManual); }
            set { SetProperty(() => RobotModeManual, value, () => { RaisePropertiesChanged(nameof(IsChanged), nameof(IsRobotModeManualOverriden)); }); }
        }

        public PriceRobotMode RobotModeManualOld
        {
            get { return GetProperty(() => RobotModeManualOld); }
            set { SetProperty(() => RobotModeManualOld, value, () => { RaisePropertiesChanged(nameof(IsChanged)); }); }
        }

        public PriceRobotMode RobotModeAuto
        {
            get { return GetProperty(() => RobotModeAuto); }
            set { SetProperty(() => RobotModeAuto, value, () => { RaisePropertiesChanged(nameof(IsChanged), nameof(IsRobotModeAutoOverriden)); }); }
        }

        public PriceRobotMode RobotModeAutoOld
        {
            get { return GetProperty(() => RobotModeAutoOld); }
            set { SetProperty(() => RobotModeAutoOld, value, () => { RaisePropertiesChanged(nameof(IsChanged)); }); }
        }

        public int CategoryId
        {
            get { return GetProperty(() => CategoryId); }
            set { SetProperty(() => CategoryId, value); }
        }

        public string CategoryName
        {
            get { return GetProperty(() => CategoryName); }
            set { SetProperty(() => CategoryName, value); }
        }

        public PromoSimpleDto Promo
        {
            get { return GetProperty(() => Promo); }
            set { SetProperty(() => Promo, value); }
        }

        public int? BonusTypeId
        {
            get { return GetProperty(() => BonusTypeId); }
            set { SetProperty(() => BonusTypeId, value, BonusTypeChanged); }
        }

        public int? BonusesToCharge
        {
            get { return GetProperty(() => BonusesToCharge); }
            set { SetProperty(() => BonusesToCharge, value, () => { RaisePropertyChanged(nameof(IsChanged)); }); }
        }

        public PriceRobotMode CategoryRobotModeManual
        {
            get { return GetProperty(() => CategoryRobotModeManual); }
            set { SetProperty(() => CategoryRobotModeManual, value, () => { RaisePropertiesChanged(nameof(IsRobotModeManualOverriden)); }); }
        }

        public PriceRobotMode CategoryRobotModeAuto
        {
            get { return GetProperty(() => CategoryRobotModeAuto); }
            set { SetProperty(() => CategoryRobotModeAuto, value, () => { RaisePropertiesChanged(nameof(IsRobotModeManualOverriden)); }); }
        }

        public IReadOnlyCollection<PropertyChangeDto> PropertyChanges
        {
            get { return GetProperty(() => PropertyChanges); }
            set { SetProperty(() => PropertyChanges, value, () => PropertyChangesButtonVisible = value?.Any() == true); }
        }

        public bool PropertyChangesButtonVisible
        {
            get { return GetProperty(() => PropertyChangesButtonVisible); }
            set { SetProperty(() => PropertyChangesButtonVisible, value); }
        }

        public bool ReadyForPriceCalculation
        {
            get { return GetProperty(() => ReadyForPriceCalculation); }
            set { SetProperty(() => ReadyForPriceCalculation, value); }
        }

        public bool IsRobotModeManualOverriden => RobotModeManual != null && CategoryRobotModeManual != null && RobotModeManual != CategoryRobotModeManual;

        public bool IsRobotModeAutoOverriden => RobotModeAuto != null && CategoryRobotModeAuto != null && RobotModeAuto != CategoryRobotModeAuto;

        public bool IsChanged => Avail != AvailOld
            || LabelRetail != LabelRetailOld
            || LabelWholesale != LabelWholesaleOld
            || RobotModeManual != RobotModeManualOld
            || RobotModeAuto != RobotModeAutoOld
            || Hotline != HotlineOld
            || Ratio != RatioOld
            || MinLeftover != MinLeftoverOld
            || Rozetka != RozetkaOld
            || Monomarket != MonomarketOld
            || FreeDelivery != FreeDeliveryOld
            || ShowInAccessories != ShowInAccessoriesOld
            || BonusTypeId != BonusTypeIdOld
            || BonusesToCharge != BonusesToChargeOld
            || MaxTradeInPrice != OldMaxTradeInPrice
            || Mining != MiningOld
            || ShowcasePickupModeIdOld != ShowcasePickupModeId
            || ReservedQuantity != ReservedQuantityOld
            || !string.Equals(PriceComment, PriceCommentOld, StringComparison.Ordinal);

        public string AvailColor
        {
            get
            {
                string hex = string.Empty;

                if (AvailOld != null && Avail != null && AvailOld.Type != Avail.Type)
                {
                    if (Avail.Type == ProductAvailabilityType.Expected)
                    {
                        hex = "FFFF00"; // yellow
                    }
                    else if (Avail.Type.Id > AvailOld.Type.Id)
                    {
                        hex = "FFC0CB"; // lightPink
                    }
                    else
                    {
                        hex = "90EE90"; // lightGreen
                    }
                }

                return hex;
            }
        }

        public int UsdCurrency { get; set; }

        public decimal UsdConversionRate { get; set; }

        public ContractorPrice[] CompetitorPrices { get; set; }

        public ContractorPrice[] SupplierPrices { get; set; }

        public ContractorPrice[] RrpPrices { get; set; }

        public ContractorPrice[] ConfiguratorPrices { get; set; }

        public ContractorPrice[] HotlineMinCompetitorClassPrices { get; set; }

        public int[] OrderSales { get; set; }

        public int[] ProductSales { get; set; }

        public double[] SalesAvgPrices { get; set; }

        public double[] PurchasesAvgPrices { get; set; }

        public double[] PromoWeights { get; set; }

        public IReadOnlyCollection<ProductPurchasesDto> Purchases { get; set; }

        public IReadOnlyCollection<PromoHistoryDto> PromoHistory { get; set; }

        public IReadOnlyCollection<ProductSalesDto> Sales { get; set; }

        public IReadOnlyCollection<SearchTemplateProduct> SearchTemplateProducts { get; set; }

        public IReadOnlyCollection<AbcClassPriceDto> HotlineAbcClassPrices { get; set; }

        public string Error => string.Empty;

        public string DisplayName => this.GetLocalName(LocalizableNameType.Ukr);

        string IDataErrorInfo.this[string columnName] => IDataErrorInfoHelper.GetErrorText(this, columnName);

        public static void BuildMetadata(MetadataBuilder<ProductPriceViewItem> builder)
        {
            builder.Property(x => x.MaxTradeInPrice)
                .MatchesRule(x => x == null || (x >= 0 && x <= 1), _ => "Значение должно быть в диапазоне 0..1");
        }

        public IEnumerable<ProductPriceDataViewItem> GetEditablePriceDatas()
        {
            yield return Telemart1PriceData;
            yield return Telemart3PriceData;
            yield return Telemart2PriceData;
            yield return Telemart4PriceData;
            yield return Telemart5PriceData;

            yield return Configurator1PriceData;
            yield return Configurator2PriceData;
            yield return Configurator3PriceData;
            yield return Configurator4PriceData;
            yield return Configurator5PriceData;
            yield return Configurator6PriceData;
            yield return Configurator7PriceData;
            yield return Configurator8PriceData;
            yield return Configurator9PriceData;
            yield return Configurator10PriceData;
        }

        public IEnumerable<string> GetErrors()
        {
            if (GetEditablePriceDatas().Any(y => y.GetPriceKind()?.Id > 0 && y.GetPriceKind()?.Id < 12 && y.GetAvailability().CanBuy && y.DisplayPrice <= 0))
            {
                yield return "Цена не должна быть 0";
            }

            if (GetEditablePriceDatas().Any(y => y.DisplayCurrencyId == Currency.UahId && y.MaxBonusesToUse > 0 && y.MaxBonusesToUse >= y.DisplayPrice))
            {
                yield return "Цена не может быть меньше либо равна максимального количества бонусов";
            }

            if (GetEditablePriceDatas().Any(y => y.MaxBonusesToUse < 0))
            {
                yield return "Максимальное количество бонусов не может быть отрицательным";
            }

            if (GetEditablePriceDatas().Any(y => y.PartialPay.HasValue && y.PartialPay is < 3 or > 25))
            {
                yield return "Значение поля ОЧ должно быть от 3 до 25";
            }

            if (GetEditablePriceDatas().Any(y => y.PartialPayPb.HasValue && y.PartialPayPb is < 1 or > 25))
            {
                yield return "Значение поля ОЧ ПриватБанк доллжно быть от 1 до 25";
            }

            if (GetEditablePriceDatas().Any(y => y.PartialPayPumb.HasValue && y.PartialPayPumb is < 1 or > 25))
            {
                yield return "Значение поля ОЧ ПУМБ доллжно быть от 1 до 25";
            }

            if (GetEditablePriceDatas().Any(y => y.PartialPayAb.HasValue && y.PartialPayAb is < 1 or > 25))
            {
                yield return "Значение поля ОЧ А-банк доллжно быть от 1 до 25";
            }

            if (BonusesToCharge < 0)
            {
                yield return "Количество начисляемых бонусов не может быть отрицательным";
            }
        }

        public void ResetPriceAndAvailChanges()
        {
            foreach (ProductPriceDataViewItem priceData in GetEditablePriceDatas())
            {
                priceData.DisplayPrice = priceData.DisplayPriceOld;
            }

            Avail = AvailOld;

            RaisePricePropertiesChanged();
        }

        public void SetDisplayCurrency(int? displayCurrencyId = null)
        {
            foreach ((string propName, ProductPriceDataViewItem propValue) in GetPriceProperties())
            {
                propValue.SetDisplayCurrency(displayCurrencyId);
                RaisePropertyChanged(propName);
            }
        }

        public CalculatePriceContext GetContext(PriceRobotMode robotMode, IReadOnlyCollection<CreditOfferDto> creditOffers, int? minPartialPayCount)
        {
            return new CalculatePriceContext(
                Id,
                ProductTypeId ?? ProductType.ProductId,
                Name,
                WarehousePriceData.GetPriceOldUsdValue(),
                (double)UsdConversionRate,
                Avail.Type == ProductAvailabilityType.InStock ? Telemart1PriceData.GetPriceUsdValue() : 0,
                Avail.Type == ProductAvailabilityType.InStock ? Telemart1PriceData.GetPriceOldUsdValue() : 0,
                Avail.Type == ProductAvailabilityType.InStock ? Telemart2PriceData.GetPriceOldUsdValue() : 0,
                Avail.Type == ProductAvailabilityType.InStock ? Telemart3PriceData.GetPriceOldUsdValue() : 0,
                Avail.Type == ProductAvailabilityType.InStock ? Telemart4PriceData.GetPriceOldUsdValue() : 0,
                Avail.Type == ProductAvailabilityType.InStock ? Telemart5PriceData.GetPriceOldUsdValue() : 0,
                Telemart1PriceData.GetTagColor().Id,
                Telemart2PriceData.GetTagColor().Id,
                Telemart3PriceData.GetTagColor().Id,
                Telemart4PriceData.GetTagColor().Id,
                Telemart5PriceData.GetTagColor().Id,
                Telemart1PriceData.PartialPay,
                Telemart2PriceData.PartialPay,
                Telemart3PriceData.PartialPay,
                Telemart4PriceData.PartialPay,
                Telemart5PriceData.PartialPay,
                Telemart1PriceData.PartialPayPb,
                Telemart2PriceData.PartialPayPb,
                Telemart3PriceData.PartialPayPb,
                Telemart4PriceData.PartialPayPb,
                Telemart5PriceData.PartialPayPb,
                Telemart1PriceData.PartialPayPumb,
                Telemart2PriceData.PartialPayPumb,
                Telemart3PriceData.PartialPayPumb,
                Telemart4PriceData.PartialPayPumb,
                Telemart5PriceData.PartialPayPumb,
                Telemart1PriceData.PartialPayAb,
                Telemart2PriceData.PartialPayAb,
                Telemart3PriceData.PartialPayAb,
                Telemart4PriceData.PartialPayAb,
                Telemart5PriceData.PartialPayAb,
                Avail.Type == ProductAvailabilityType.InStock ? Telemart1PriceData.GetPricePrevOldUsdValue() : 0,
                Configurator1PriceData.GetPriceOldUsdValue(),
                Configurator2PriceData.GetPriceOldUsdValue(),
                Configurator3PriceData.GetPriceOldUsdValue(),
                Configurator4PriceData.GetPriceOldUsdValue(),
                Configurator5PriceData.GetPriceOldUsdValue(),
                Configurator6PriceData.GetPriceOldUsdValue(),
                Configurator7PriceData.GetPriceOldUsdValue(),
                Configurator8PriceData.GetPriceOldUsdValue(),
                Configurator9PriceData.GetPriceOldUsdValue(),
                Configurator10PriceData.GetPriceOldUsdValue(),
                (double)(HotlineMinPriceUsd ?? 0),
                (double)(HotlinePriceUsd ?? 0),
                (double)(PriceTransit ?? 0),
                BonusTypeId,
                BonusesToCharge,
                Telemart1PriceData.MaxBonusesToUse,
                Telemart2PriceData.MaxBonusesToUse,
                Telemart3PriceData.MaxBonusesToUse,
                Telemart4PriceData.MaxBonusesToUse,
                Telemart5PriceData.MaxBonusesToUse,
                HotlinePosition ?? 0,
                Density14Days,
                Density1Month,
                DaysInStock ?? 0,
                InStock,
                Conversion,
                DaysFromLastSale,
                LabelRetailOld?.Id ?? 0,
                AvailOld.Id,
                Rozetka == 1,
                Monomarket == 1,
                FreeDelivery,
                ShowInAccessories,
                (double)LastSalePriceUsd,
                SalesCurrentMonth,
                SalesLastMonth,
                SalesBeforeLastMonth,
                OrdersCurrentMonth,
                OrdersLastMonth,
                OrdersBeforeLastMonth,
                Visits,
                Hotline == 1,
                MinLeftover ?? 0,
                PlannedLeftover,
                ShowcasesCapacity,
                ParentPrice1?.GetPriceOldUsdValue(),
                ParentPrice2?.GetPriceOldUsdValue(),
                ParentPrice3?.GetPriceOldUsdValue(),
                ParentPrice4?.GetPriceOldUsdValue(),
                ParentPrice5?.GetPriceOldUsdValue(),
                AssembledComputerRuleBasePrice1?.GetPriceOldUsdValue(),
                AssembledComputerRuleBasePrice2?.GetPriceOldUsdValue(),
                AssembledComputerRuleBasePrice3?.GetPriceOldUsdValue(),
                AssembledComputerRuleBasePrice4?.GetPriceOldUsdValue(),
                AssembledComputerRuleBasePrice5?.GetPriceOldUsdValue(),
                WarrantyRetail,
                MaxTradeInPrice,
                Mining,
                ShowcasePickupModeId,
                SupplierPrices,
                CompetitorPrices,
                RrpPrices,
                ConfiguratorPrices,
                HotlineMinCompetitorClassPrices,
                OrderSales,
                ProductSales,
                SalesAvgPrices,
                PurchasesAvgPrices,
                PromoWeights,
                robotMode.Id,
                true,
                F2Markup,
                PlannedMarkup,
                UsePlannedMarkupInAutoShowcase,
                ReservedQuantity,
                ShowcasesStock,
                SearchTemplateProducts,
                Preorder,
                (double)MinProductMarginPercent,
                (double?)MinCategoryMarginPercent,
                creditOffers,
                minPartialPayCount);
        }

        public void SetCalculatePriceResult(IDictionaries dictionaries, ProductPriceSaveDto saveDto, IPriceConverter priceConverter)
        {
            if (Avail.Id != saveDto.AvailId)
            {
                AvailModifiedBy = Constants.ParserEmployeeId;
            }

            Hotline = saveDto.Hotline;
            MinLeftover = saveDto.MinLeftover;
            PlannedLeftover = saveDto.PlannedLeftover;
            FreeDelivery = saveDto.FreeDelivery;
            ShowInAccessories = saveDto.ShowInAccessories;
            Rozetka = saveDto.Rozetka;
            Monomarket = saveDto.Monomarket;
            BonusesToCharge = saveDto.BonusesToCharge;
            LabelRetail = dictionaries.GetItemById<ProductLabel>(saveDto.LabelRetailId ?? 0);
            LabelWholesale = dictionaries.GetItemById<ProductLabel>(saveDto.LabelRetailId ?? 0);
            Avail = dictionaries.GetItemById<ProductAvailability>(saveDto.AvailId);
            BonusTypeId = saveDto.BonusTypeId;
            MaxTradeInPrice = saveDto.MaxTradeInPrice;
            ReservedQuantity = saveDto.ReservedQuantity ?? 0;
            PropertyChanges = saveDto.PropertyChanges;

            ProductPriceDataSaveDto price1Dto = saveDto.Prices.First(x => x.PriceTypeId == ProductPriceKind.Telemart1);
            ProductPriceDataSaveDto price2Dto = saveDto.Prices.First(x => x.PriceTypeId == ProductPriceKind.Telemart2);
            ProductPriceDataSaveDto price3Dto = saveDto.Prices.First(x => x.PriceTypeId == ProductPriceKind.Telemart3);
            ProductPriceDataSaveDto price4Dto = saveDto.Prices.First(x => x.PriceTypeId == ProductPriceKind.Telemart4);
            ProductPriceDataSaveDto price5Dto = saveDto.Prices.First(x => x.PriceTypeId == ProductPriceKind.Telemart5);

            Telemart1PriceData.SetPriceUsdValue((double)price1Dto.Price, (double)(price1Dto.PricePrev ?? 0), priceConverter);
            Telemart2PriceData.SetPriceUsdValue((double)price2Dto.Price, (double)(price2Dto.PricePrev ?? 0), priceConverter);
            Telemart3PriceData.SetPriceUsdValue((double)price3Dto.Price, (double)(price3Dto.PricePrev ?? 0), priceConverter);
            Telemart4PriceData.SetPriceUsdValue((double)price4Dto.Price, (double)(price4Dto.PricePrev ?? 0), priceConverter);
            Telemart5PriceData.SetPriceUsdValue((double)price5Dto.Price, (double)(price5Dto.PricePrev ?? 0), priceConverter);

            Telemart1PriceData.PricePoliticName = price1Dto.PricePoliticName;

            Telemart1PriceData.MaxBonusesToUse = price1Dto.MaxBonusesToUse;
            Telemart2PriceData.MaxBonusesToUse = price2Dto.MaxBonusesToUse;
            Telemart3PriceData.MaxBonusesToUse = price3Dto.MaxBonusesToUse;
            Telemart4PriceData.MaxBonusesToUse = price4Dto.MaxBonusesToUse;
            Telemart5PriceData.MaxBonusesToUse = price5Dto.MaxBonusesToUse;

            Telemart1PriceData.PartialPay = price1Dto.PartialPay;
            Telemart2PriceData.PartialPay = price2Dto.PartialPay;
            Telemart3PriceData.PartialPay = price3Dto.PartialPay;
            Telemart4PriceData.PartialPay = price4Dto.PartialPay;
            Telemart5PriceData.PartialPay = price5Dto.PartialPay;

            Telemart1PriceData.PartialPayPb = price1Dto.PartialPayPb;
            Telemart2PriceData.PartialPayPb = price2Dto.PartialPayPb;
            Telemart3PriceData.PartialPayPb = price3Dto.PartialPayPb;
            Telemart4PriceData.PartialPayPb = price4Dto.PartialPayPb;
            Telemart5PriceData.PartialPayPb = price5Dto.PartialPayPb;

            Telemart1PriceData.PartialPayPumb = price1Dto.PartialPayPumb;
            Telemart2PriceData.PartialPayPumb = price2Dto.PartialPayPumb;
            Telemart3PriceData.PartialPayPumb = price3Dto.PartialPayPumb;
            Telemart4PriceData.PartialPayPumb = price4Dto.PartialPayPumb;
            Telemart5PriceData.PartialPayPumb = price5Dto.PartialPayPumb;

            Telemart1PriceData.PartialPayAb = price1Dto.PartialPayAb;
            Telemart2PriceData.PartialPayAb = price2Dto.PartialPayAb;
            Telemart3PriceData.PartialPayAb = price3Dto.PartialPayAb;
            Telemart4PriceData.PartialPayAb = price4Dto.PartialPayAb;
            Telemart5PriceData.PartialPayAb = price5Dto.PartialPayAb;

            Telemart1PriceData.TagColor = DictionaryItems<TagColor>.Value.First(x => x.Id == price1Dto.TagColorId);
            Telemart2PriceData.TagColor = DictionaryItems<TagColor>.Value.First(x => x.Id == price2Dto.TagColorId);
            Telemart3PriceData.TagColor = DictionaryItems<TagColor>.Value.First(x => x.Id == price3Dto.TagColorId);
            Telemart4PriceData.TagColor = DictionaryItems<TagColor>.Value.First(x => x.Id == price4Dto.TagColorId);
            Telemart5PriceData.TagColor = DictionaryItems<TagColor>.Value.First(x => x.Id == price5Dto.TagColorId);

            RaisePricePropertiesChanged();

            RaisePropertiesChanged(nameof(LabelRetail), nameof(LabelWholesale), nameof(Rozetka), nameof(Monomarket), nameof(FreeDelivery), nameof(ShowInAccessories));
        }

        public void SetCalculatePriceResult(IDictionaries dictionaries, CalculatePriceResultData result, IPriceConverter priceConverter)
        {
            if (Avail.Id != result.AvailId)
            {
                AvailModifiedBy = Constants.ParserEmployeeId;
            }

            Avail = dictionaries.GetItemById<ProductAvailability>(result.AvailId);

            Telemart1PriceData.SetPriceUsdValue(result.Price1, result.PricePrev, priceConverter);
            Telemart2PriceData.SetPriceUsdValue(result.Price2, result.PricePrev, priceConverter);
            Telemart3PriceData.SetPriceUsdValue(result.Price3, result.PricePrev, priceConverter);
            Telemart4PriceData.SetPriceUsdValue(result.Price4, result.PricePrev, priceConverter);
            Telemart5PriceData.SetPriceUsdValue(result.Price5, result.PricePrev, priceConverter);

            Configurator1PriceData.SetPriceUsdValue(result.PriceConfigurator1, result.PricePrev, priceConverter);
            Configurator2PriceData.SetPriceUsdValue(result.PriceConfigurator2, result.PricePrev, priceConverter);
            Configurator3PriceData.SetPriceUsdValue(result.PriceConfigurator3, result.PricePrev, priceConverter);
            Configurator4PriceData.SetPriceUsdValue(result.PriceConfigurator4, result.PricePrev, priceConverter);
            Configurator5PriceData.SetPriceUsdValue(result.PriceConfigurator5, result.PricePrev, priceConverter);
            Configurator6PriceData.SetPriceUsdValue(result.PriceConfigurator6, result.PricePrev, priceConverter);
            Configurator7PriceData.SetPriceUsdValue(result.PriceConfigurator7, result.PricePrev, priceConverter);
            Configurator8PriceData.SetPriceUsdValue(result.PriceConfigurator8, result.PricePrev, priceConverter);
            Configurator9PriceData.SetPriceUsdValue(result.PriceConfigurator9, result.PricePrev, priceConverter);
            Configurator10PriceData.SetPriceUsdValue(result.PriceConfigurator10, result.PricePrev, priceConverter);

            LabelRetail = result.LabelId.HasValue ? dictionaries.GetItemById<ProductLabel>(result.LabelId.Value) : null;
            Rozetka = result.Rozetka ? 1 : 0;
            Monomarket = result.Monomarket ? 1 : 0;
            FreeDelivery = result.FreeDelivery;
            ShowInAccessories = result.ShowInAccessories;
            PlannedLeftover = result.PlannedLeftover;
            Hotline = result.Hotline ? 1 : 0;
            MinLeftover = result.MinLeftover == 0 ? null : result.MinLeftover;
            MaxTradeInPrice = result.MaxTradeInPrice;
            Mining = result.Mining;
            ReservedQuantity = result.ReservedStocks;

            BonusTypeId = result.BonusType;
            BonusesToCharge = result.BonusEarn;

            Telemart1PriceData.MaxBonusesToUse = result.BonusSpend1 ?? 0;
            Telemart2PriceData.MaxBonusesToUse = result.BonusSpend2 ?? 0;
            Telemart3PriceData.MaxBonusesToUse = result.BonusSpend3 ?? 0;
            Telemart4PriceData.MaxBonusesToUse = result.BonusSpend4 ?? 0;
            Telemart5PriceData.MaxBonusesToUse = result.BonusSpend5 ?? 0;

            Telemart1PriceData.PartialPay = result.PartialPay1;
            Telemart2PriceData.PartialPay = result.PartialPay2;
            Telemart3PriceData.PartialPay = result.PartialPay3;
            Telemart4PriceData.PartialPay = result.PartialPay4;
            Telemart5PriceData.PartialPay = result.PartialPay5;

            Telemart1PriceData.PartialPayPb = result.PartialPayPb1;
            Telemart2PriceData.PartialPayPb = result.PartialPayPb2;
            Telemart3PriceData.PartialPayPb = result.PartialPayPb3;
            Telemart4PriceData.PartialPayPb = result.PartialPayPb4;
            Telemart5PriceData.PartialPayPb = result.PartialPayPb5;

            Telemart1PriceData.PartialPayPumb = result.PartialPayPumb1;

            Telemart1PriceData.TagColor = DictionaryItems<TagColor>.Value.First(x => x.Id == result.TagColorId1);
            Telemart2PriceData.TagColor = DictionaryItems<TagColor>.Value.First(x => x.Id == result.TagColorId2);
            Telemart3PriceData.TagColor = DictionaryItems<TagColor>.Value.First(x => x.Id == result.TagColorId3);
            Telemart4PriceData.TagColor = DictionaryItems<TagColor>.Value.First(x => x.Id == result.TagColorId4);
            Telemart5PriceData.TagColor = DictionaryItems<TagColor>.Value.First(x => x.Id == result.TagColorId5);

            RaisePricePropertiesChanged();

            RaisePropertiesChanged(nameof(LabelRetail), nameof(LabelWholesale), nameof(Rozetka), nameof(Monomarket), nameof(FreeDelivery), nameof(ShowInAccessories));
        }

        public void RaisePricePropertiesChanged()
        {
            RaisePropertiesChanged(GetPriceProperties().Select(x => x.PropName).ToArray());
        }

        private IEnumerable<(string PropName, ProductPriceDataViewItem PropValue)> GetPriceProperties()
        {
            yield return (nameof(WarehousePriceData), WarehousePriceData);
            yield return (nameof(Telemart3PriceData), Telemart3PriceData);
            yield return (nameof(Telemart1PriceData), Telemart1PriceData);
            yield return (nameof(Telemart2PriceData), Telemart2PriceData);
            yield return (nameof(Telemart4PriceData), Telemart4PriceData);
            yield return (nameof(Telemart5PriceData), Telemart5PriceData);
            yield return (nameof(PriceInPriceData), PriceInPriceData);
        }

        private void BonusTypeChanged()
        {
            if (BonusTypeId == null)
            {
                BonusesToCharge = null;
            }

            RaisePropertyChanged(nameof(IsChanged));
        }
    }
}