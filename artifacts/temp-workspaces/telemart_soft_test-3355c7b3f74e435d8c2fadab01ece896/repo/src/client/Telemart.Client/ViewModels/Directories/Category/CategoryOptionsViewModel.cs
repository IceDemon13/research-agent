using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.Utils;
using Telemart.Client.Data.Requests.Features.Admitad;
using Telemart.Client.Data.Requests.Features.Category;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.Requests.Features.FeatureGroup;
using Telemart.Client.Data.Requests.Features.Tag;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.CategoryOptions;
using Telemart.Client.TransferObjects.Content;
using Telemart.Client.TransferObjects.Paging;

namespace Telemart.Client.ViewModels.Directories.Category
{
    [MetadataType(typeof(CategoryOptionsViewModelMetadata))]
    public sealed class CategoryOptionsViewModel : ViewModelBase, IDataErrorInfo
    {
        private const string CategoryOptionModelEnding = "OptionModel";
        private const CategoryOptionViewModel<int> NamesSource = null;

        private bool? isChanged = false;

        private List<CategoryMappingDto> mappings;

        public CategoryOptionsViewModel(IWebClient webClient, IDictionaries dictionaries, bool isEditingAllowed = true, bool isOptionsLockedByCurrentEmployee = false)
            : this()
        {
            WebClient = webClient ?? throw new ArgumentNullException(nameof(webClient));
            Dictionaries = dictionaries ?? throw new ArgumentNullException(nameof(dictionaries));
            IsEditingAllowed = isEditingAllowed;
            IsOptionsLockedByCurrentEmployee = isOptionsLockedByCurrentEmployee;

            PropertyChanged += OnPropertyChanged;
        }

        public CategoryOptionsViewModel()
        {
        }

        #region Editor Values

        public ObservableRangeCollection<ActiveValue> ActiveValues { get; } = new ObservableRangeCollection<ActiveValue>(ActiveValue.GetCategoryAvailableValues());

        public ObservableRangeCollection<Currency> Currencies { get; } = new ObservableRangeCollection<Currency>();

        public ObservableRangeCollection<EmployeeViewItem> Employees { get; } = new ObservableRangeCollection<EmployeeViewItem>();

        public ObservableRangeCollection<WarrantyType> WarrantyTypes { get; } = new ObservableRangeCollection<WarrantyType>();

        public ObservableRangeCollection<ProductKind> Kinds { get; } = new ObservableRangeCollection<ProductKind>();

        public ObservableRangeCollection<PriceRobotMode> PriceRobotManualModes { get; } = new ObservableRangeCollection<PriceRobotMode>();

        public ObservableRangeCollection<PriceRobotMode> PriceRobotAutoModes { get; } = new ObservableRangeCollection<PriceRobotMode>();

        public ObservableRangeCollection<TagFormatDto> TagFormats { get; } = new ObservableRangeCollection<TagFormatDto>();

        public ObservableRangeCollection<TaxRate> TaxRates { get; } = new ObservableRangeCollection<TaxRate>();

        public ObservableRangeCollection<UsdCurrency> UsdCurrencies { get; } = new ObservableRangeCollection<UsdCurrency>();

        public ObservableRangeCollection<Warranty> Warranties { get; } = new ObservableRangeCollection<Warranty>();

        public ObservableRangeCollection<CategoryType> Types { get; } = new ObservableRangeCollection<CategoryType>();

        public ObservableRangeCollection<AdProviderTariffCodeDto> AdmitadTariffCodes { get; } = new ObservableRangeCollection<AdProviderTariffCodeDto>();

        public ObservableRangeCollection<AdProviderTariffCodeDto> SalesDoublerTariffCodes { get; } = new ObservableRangeCollection<AdProviderTariffCodeDto>();

        public ObservableRangeCollection<ReferralDiscountCode> ReferralDiscountCodes { get; } = new ObservableRangeCollection<ReferralDiscountCode>();

        public ObservableRangeCollection<FeatureDto> Features { get; } = new ObservableRangeCollection<FeatureDto>();

        public bool IsChanged
        {
            get
            {
                if (isChanged == null)
                {
                    isChanged = GetChangedOptionsSaveDtos().Any();
                }

                return isChanged.Value;
            }

            set
            {
                isChanged = value;
                RaisePropertyChanged(nameof(IsChanged));
            }
        }

        #endregion

        public CategoryFullDto Category { get; private set; }

        public CategoryFullDto Parent { get; private set; }

        public CategoryFullDto Target { get; private set; }

        public bool IsEditingAllowed { get; }

        public bool IsOptionsLockedByCurrentEmployee
        {
            get { return GetProperty(() => IsOptionsLockedByCurrentEmployee); }
            set { SetProperty(() => IsOptionsLockedByCurrentEmployee, value); }
        }

        public bool IsLongOperationInProgress
        {
            get { return GetProperty(() => IsLongOperationInProgress); }
            set { SetProperty(() => IsLongOperationInProgress, value); }
        }

        public bool CanChangeAnyField
        {
            get { return GetProperty(() => CanChangeAnyField); }
            set { SetProperty(() => CanChangeAnyField, value); }
        }

        public int Id
        {
            get { return Target.Id; }
            set { Target.Id = value; }
        }

        public int ParentId
        {
            get { return Target.ParentId; }
            set { Target.ParentId = value; }
        }

        public int Category1C
        {
            get { return Target.Category1C; }
            set { Target.Category1C = value; }
        }

        public string Name
        {
            get
            {
                return Target.Name;
            }

            set
            {
                Target.Name = value;
                NameOptionModel.CurrentValue = value;
                RaisePropertiesChanged(nameof(Name), nameof(NameOptionModel));
            }
        }

        public CategoryOptionViewModel<string> NameOptionModel { get; set; }

        public string NameUkr
        {
            get
            {
                return Target.NameUkr;
            }

            set
            {
                Target.NameUkr = value;
                NameUkrOptionModel.CurrentValue = value;
                RaisePropertiesChanged(nameof(NameUkr), nameof(NameUkrOptionModel));
            }
        }

        public CategoryOptionViewModel<string> NameUkrOptionModel { get; set; }

        public string NameEn
        {
            get
            {
                return Target.NameEn;
            }

            set
            {
                Target.NameEn = value;
                NameEnOptionModel.CurrentValue = value;
                RaisePropertiesChanged(nameof(NameEn), nameof(NameEnOptionModel));
            }
        }

        public CategoryOptionViewModel<string> NameEnOptionModel { get; set; }

        public string NameFull
        {
            get { return Target.NameFull; }
            set { Target.NameFull = value; }
        }

        public string NameFullUkr
        {
            get { return Target.NameFullUkr; }
            set { Target.NameFullUkr = value; }
        }

        public string NameFullEn
        {
            get { return Target.NameFullEn; }
            set { Target.NameFullEn = value; }
        }

        public bool IsParent
        {
            get { return Target.IsParent; }
            set { Target.IsParent = value; }
        }

        public double Active
        {
            get
            {
                return Target.Active;
            }

            set
            {
                Target.Active = value;
                ActiveOptionModel.CurrentValue = value;
                RaisePropertiesChanged(nameof(Active), nameof(ActiveOptionModel));
            }
        }

        public CategoryOptionViewModel<double> ActiveOptionModel { get; set; }

        public bool UseInTradeIn
        {
            get
            {
                return Target.UseInTradeIn;
            }

            set
            {
                Target.UseInTradeIn = value;
                UseInTradeInOptionModel.CurrentValue = value;
                RaisePropertiesChanged(nameof(UseInTradeIn), nameof(UseInTradeInOptionModel));
            }
        }

        public CategoryOptionViewModel<bool> UseInTradeInOptionModel { get; set; }

        public decimal TradeInServiceCost
        {
            get
            {
                return Target.TradeInServiceCost;
            }

            set
            {
                decimal clampedValue = Math.Clamp(value, 0.0M, 999.99M);

                if (Target.TradeInServiceCostCurrencyId == Client.Dictionaries.Currency.UahId)
                {
                    clampedValue = Math.Ceiling(clampedValue);
                }

                Target.TradeInServiceCost = clampedValue;
                TradeInServiceCostOptionModel.CurrentValue = clampedValue;

                RaisePropertiesChanged(nameof(TradeInServiceCost), nameof(TradeInServiceCostOptionModel));
            }
        }

        public CategoryOptionViewModel<decimal> TradeInServiceCostOptionModel { get; set; }

        public int TradeInServiceCostCurrencyId
        {
            get
            {
                return Target.TradeInServiceCostCurrencyId;
            }

            set
            {
                Target.TradeInServiceCostCurrencyId = value;
                TradeInServiceCostCurrencyOptionModel.CurrentValue = value;

                TradeInServiceCost = TradeInServiceCost;

                RaisePropertiesChanged(nameof(TradeInServiceCostCurrencyId), nameof(TradeInServiceCostCurrencyOptionModel));
            }
        }

        public CategoryOptionViewModel<int> TradeInServiceCostCurrencyOptionModel { get; set; }

        public decimal? MinCategoryMarginPercent
        {
            get
            {
                return Target.MinCategoryMarginPercent;
            }

            set
            {
                Target.MinCategoryMarginPercent = value;
                MinCategoryMarginPercentModel.CurrentValue = value;
                RaisePropertiesChanged(nameof(MinCategoryMarginPercent), nameof(MinCategoryMarginPercentModel));
            }
        }

        public CategoryOptionViewModel<decimal?> MinCategoryMarginPercentModel { get; set; }

        public int YandexMarketHid
        {
            get
            {
                return Target.YandexMarketHid;
            }

            set
            {
                Target.YandexMarketHid = value;
                YandexMarketHidOptionModel.CurrentValue = value;
                RaisePropertyChanged(nameof(YandexMarketHid));
                RaisePropertyChanged(nameof(YandexMarketHidOptionModel));
            }
        }

        public CategoryOptionViewModel<int> YandexMarketHidOptionModel { get; set; }

        public string Manufactor
        {
            get
            {
                return Target.Manufactor;
            }

            set
            {
                Target.Manufactor = value;
                ManufactorOptionModel.CurrentValue = value;
                RaisePropertyChanged(nameof(Manufactor));
                RaisePropertyChanged(nameof(ManufactorOptionModel));
            }
        }

        public CategoryOptionViewModel<string> ManufactorOptionModel { get; set; }

        public string RobotScript
        {
            get
            {
                return Target.RobotScript;
            }

            set
            {
                Target.RobotScript = value;
                RobotScriptOptionModel.CurrentValue = value;
                RaisePropertiesChanged(nameof(RobotScript), nameof(RobotScriptOptionModel));
            }
        }

        public CategoryOptionViewModel<string> RobotScriptOptionModel { get; set; }

        public string RobotScriptParameters
        {
            get
            {
                return Target.RobotScriptParameters;
            }

            set
            {
                Target.RobotScriptParameters = value;
                RobotScriptParametersOptionModel.CurrentValue = value;
                RaisePropertiesChanged(nameof(RobotScriptParameters), nameof(RobotScriptParametersOptionModel));
            }
        }

        public CategoryOptionViewModel<string> RobotScriptParametersOptionModel { get; set; }

        public int RobotModeManualId
        {
            get
            {
                return Target.RobotModeManualId;
            }

            set
            {
                Target.RobotModeManualId = value;
                RobotModeManualIdOptionModel.CurrentValue = value;
                RaisePropertyChanged(nameof(RobotModeManualId));
                RaisePropertyChanged(nameof(RobotModeManualIdOptionModel));
            }
        }

        public CategoryOptionViewModel<int> RobotModeManualIdOptionModel { get; set; }

        public int RobotModeAutoId
        {
            get
            {
                return Target.RobotModeAutoId;
            }

            set
            {
                Target.RobotModeAutoId = value;
                RobotModeAutoIdOptionModel.CurrentValue = value;
                RaisePropertyChanged(nameof(RobotModeAutoId));
                RaisePropertyChanged(nameof(RobotModeAutoIdOptionModel));
            }
        }

        public CategoryOptionViewModel<int> RobotModeAutoIdOptionModel { get; set; }

        public bool Hotline
        {
            get
            {
                return Target.Hotline;
            }

            set
            {
                Target.Hotline = value;
                HotlineOptionModel.CurrentValue = value;
                RaisePropertyChanged(nameof(Hotline));
                RaisePropertyChanged(nameof(HotlineOptionModel));
            }
        }

        public CategoryOptionViewModel<bool> HotlineOptionModel { get; set; }

        public int EmployeeId
        {
            get
            {
                return Target.EmployeeId;
            }

            set
            {
                Target.EmployeeId = value;
                EmployeeIdOptionModel.CurrentValue = value;
                RaisePropertyChanged(nameof(EmployeeId));
                RaisePropertyChanged(nameof(EmployeeIdOptionModel));
            }
        }

        public CategoryOptionViewModel<int> EmployeeIdOptionModel { get; set; }

        public int EmployeeSupId
        {
            get
            {
                return Target.EmployeeSupId;
            }

            set
            {
                Target.EmployeeSupId = value;
                EmployeeSupIdOptionModel.CurrentValue = value;
                RaisePropertiesChanged(nameof(EmployeeSupId), nameof(EmployeeSupIdOptionModel));
            }
        }

        public CategoryOptionViewModel<int> EmployeeSupIdOptionModel { get; set; }

        public int? WarrantyTypeId
        {
            get
            {
                return Target.WarrantyTypeId;
            }

            set
            {
                Target.WarrantyTypeId = value;
                WarrantyTypeIdOptionModel.CurrentValue = value;
                RaisePropertiesChanged(nameof(WarrantyTypeId), nameof(WarrantyTypeIdOptionModel));
            }
        }

        public CategoryOptionViewModel<int?> WarrantyTypeIdOptionModel { get; set; }

        public string Kind
        {
            get
            {
                return Target.Kind;
            }

            set
            {
                Target.Kind = value;
                KindOptionModel.CurrentValue = value;
                RaisePropertyChanged(nameof(Kind));
                RaisePropertyChanged(nameof(KindOptionModel));
            }
        }

        public CategoryOptionViewModel<string> KindOptionModel { get; set; }

        public string Currency
        {
            get
            {
                return Target.Currency;
            }

            set
            {
                Target.Currency = value;
                CurrencyOptionModel.CurrentValue = value;
                RaisePropertyChanged(nameof(Currency));
                RaisePropertyChanged(nameof(CurrencyOptionModel));
            }
        }

        public CategoryOptionViewModel<string> CurrencyOptionModel { get; set; }

        public int UsdCurrency
        {
            get
            {
                return Target.UsdCurrency;
            }

            set
            {
                Target.UsdCurrency = value;
                UsdCurrencyOptionModel.CurrentValue = value;
                RaisePropertyChanged(nameof(UsdCurrency));
                RaisePropertyChanged(nameof(UsdCurrencyOptionModel));
            }
        }

        public CategoryOptionViewModel<int> UsdCurrencyOptionModel { get; set; }

        public bool ShortNamesFor1C
        {
            get
            {
                return Target.ShortNamesFor1C;
            }

            set
            {
                Target.ShortNamesFor1C = value;
                ShortNamesFor1CModel.CurrentValue = value;
                RaisePropertiesChanged(nameof(ShortNamesFor1C), nameof(ShortNamesFor1CModel));
            }
        }

        public CategoryOptionViewModel<bool> ShortNamesFor1CModel { get; set; }

        public decimal PlannedMarkup
        {
            get
            {
                return Target.PlannedMarkup;
            }

            set
            {
                Target.PlannedMarkup = value;
                PlannedMarkupModel.CurrentValue = value;
                RaisePropertiesChanged(nameof(PlannedMarkup), nameof(PlannedMarkupModel));
            }
        }

        public CategoryOptionViewModel<decimal> PlannedMarkupModel { get; set; }

        public bool UseNewRobot
        {
            get
            {
                return Target.UseNewRobot;
            }

            set
            {
                Target.UseNewRobot = value;
                UseNewRobotModel.CurrentValue = value;
                RaisePropertiesChanged(nameof(UseNewRobot), nameof(UseNewRobotModel));
            }
        }

        public CategoryOptionViewModel<bool> UseNewRobotModel { get; set; }

        public bool UsePlannedMarkupInAutoShowcase
        {
            get
            {
                return Target.UsePlannedMarkupInAutoShowcase;
            }

            set
            {
                Target.UsePlannedMarkupInAutoShowcase = value;
                UsePlannedMarkupInAutoShowcaseModel.CurrentValue = value;
                RaisePropertiesChanged(nameof(UsePlannedMarkupInAutoShowcase), nameof(UsePlannedMarkupInAutoShowcaseModel));
            }
        }

        public CategoryOptionViewModel<bool> UsePlannedMarkupInAutoShowcaseModel { get; set; }

        public int ShowcaseSkuLimit
        {
            get
            {
                return Target.ShowcaseSkuLimit;
            }

            set
            {
                Target.ShowcaseSkuLimit = value;
                ShowcaseSkuLimitModel.CurrentValue = value;
                RaisePropertiesChanged(nameof(ShowcaseSkuLimit), nameof(ShowcaseSkuLimitModel));
            }
        }

        public CategoryOptionViewModel<int> ShowcaseSkuLimitModel { get; set; }

        public int WarrantyRetailId
        {
            get
            {
                return Target.WarrantyRetailId;
            }

            set
            {
                Target.WarrantyRetailId = value;
                WarrantyRetailIdOptionModel.CurrentValue = value;
                RaisePropertiesChanged(nameof(WarrantyRetailId), nameof(WarrantyRetailIdOptionModel));
            }
        }

        public CategoryOptionViewModel<int> WarrantyRetailIdOptionModel { get; set; }

        public int WarrantyWholesaleId
        {
            get
            {
                return Target.WarrantyWholesaleId;
            }

            set
            {
                Target.WarrantyWholesaleId = value;
                WarrantyWholesaleIdOptionModel.CurrentValue = value;
                RaisePropertiesChanged(nameof(WarrantyWholesaleId), nameof(WarrantyWholesaleIdOptionModel));
            }
        }

        public CategoryOptionViewModel<int> WarrantyWholesaleIdOptionModel { get; set; }

        public double WeightEstimated
        {
            get
            {
                return Target.WeightEstimated;
            }

            set
            {
                Target.WeightEstimated = value;
                WeightEstimatedOptionModel.CurrentValue = value;
                RaisePropertyChanged(nameof(WeightEstimated));
                RaisePropertyChanged(nameof(WeightEstimatedOptionModel));
            }
        }

        public CategoryOptionViewModel<double> WeightEstimatedOptionModel { get; set; }

        public bool FreeDelivery
        {
            get
            {
                return Target.FreeDelivery;
            }

            set
            {
                Target.FreeDelivery = value;
                FreeDeliveryOptionModel.CurrentValue = value;
                RaisePropertyChanged(nameof(FreeDelivery));
                RaisePropertyChanged(nameof(FreeDeliveryOptionModel));
            }
        }

        public CategoryOptionViewModel<bool> FreeDeliveryOptionModel { get; set; }

        public bool PrintWarrantyCard
        {
            get
            {
                return Target.PrintWarrantyCard;
            }

            set
            {
                Target.PrintWarrantyCard = value;
                PrintWarrantyCardOptionModel.CurrentValue = value;
                RaisePropertyChanged(nameof(PrintWarrantyCard));
                RaisePropertyChanged(nameof(PrintWarrantyCardOptionModel));
            }
        }

        public CategoryOptionViewModel<bool> PrintWarrantyCardOptionModel { get; set; }

        public bool KeepSerial
        {
            get
            {
                return Target.KeepSerial;
            }

            set
            {
                Target.KeepSerial = value;
                KeepSerialOptionModel.CurrentValue = value;
                RaisePropertyChanged(nameof(KeepSerial));
                RaisePropertyChanged(nameof(KeepSerialOptionModel));
            }
        }

        public CategoryOptionViewModel<bool> KeepSerialOptionModel { get; set; }

        public bool KeepPn
        {
            get
            {
                return Target.KeepPn;
            }

            set
            {
                Target.KeepPn = value;
                KeepPnOptionModel.CurrentValue = value;
                RaisePropertyChanged(nameof(KeepPn));
                RaisePropertyChanged(nameof(KeepPnOptionModel));
            }
        }

        public CategoryOptionViewModel<bool> KeepPnOptionModel { get; set; }

        public bool SelfBarcode
        {
            get
            {
                return Target.SelfBarcode;
            }

            set
            {
                Target.SelfBarcode = value;
                SelfBarcodeOptionModel.CurrentValue = value;
                RaisePropertiesChanged(nameof(SelfBarcode), nameof(SelfBarcodeOptionModel));
            }
        }

        public CategoryOptionViewModel<bool> SelfBarcodeOptionModel { get; set; }

        public bool KeepDimensions
        {
            get
            {
                return Target.KeepDimensions;
            }

            set
            {
                Target.KeepDimensions = value;
                KeepDimensionsOptionModel.CurrentValue = value;
                RaisePropertiesChanged(nameof(KeepDimensions), nameof(KeepDimensionsOptionModel));
            }
        }

        public CategoryOptionViewModel<bool> KeepDimensionsOptionModel { get; set; }

        public bool StickerFragile
        {
            get
            {
                return Target.StickerFragile;
            }

            set
            {
                Target.StickerFragile = value;
                StickerFragileOptionModel.CurrentValue = value;
                RaisePropertiesChanged(nameof(StickerFragile), nameof(StickerFragileOptionModel));
            }
        }

        public CategoryOptionViewModel<bool> StickerFragileOptionModel { get; set; }

        public bool StickerThisWayUp
        {
            get
            {
                return Target.StickerThisWayUp;
            }

            set
            {
                Target.StickerThisWayUp = value;
                StickerThisWayUpOptionModel.CurrentValue = value;
                RaisePropertiesChanged(nameof(StickerThisWayUp), nameof(StickerThisWayUpOptionModel));
            }
        }

        public CategoryOptionViewModel<bool> StickerThisWayUpOptionModel { get; set; }

        public int DaysToFill
        {
            get
            {
                return Target.DaysToFill;
            }

            set
            {
                Target.DaysToFill = value;
                DaysToFillOptionModel.CurrentValue = value;
                RaisePropertiesChanged(nameof(DaysToFill), nameof(DaysToFillOptionModel));
            }
        }

        public CategoryOptionViewModel<int> DaysToFillOptionModel { get; set; }

        public bool NeedContent
        {
            get
            {
                return Target.NeedContent;
            }

            set
            {
                Target.NeedContent = value;
                NeedContentOptionModel.CurrentValue = value;
                RaisePropertyChanged(nameof(NeedContent));
                RaisePropertyChanged(nameof(NeedContentOptionModel));
            }
        }

        public CategoryOptionViewModel<bool> NeedContentOptionModel { get; set; }

        public bool NeedVideo
        {
            get
            {
                return Target.NeedVideo;
            }

            set
            {
                Target.NeedVideo = value;
                NeedVideoOptionModel.CurrentValue = value;
                RaisePropertyChanged(nameof(NeedVideo));
                RaisePropertyChanged(nameof(NeedVideoOptionModel));
            }
        }

        public CategoryOptionViewModel<bool> NeedVideoOptionModel { get; set; }

        public bool NeedComplect
        {
            get
            {
                return Target.NeedComplect;
            }

            set
            {
                Target.NeedComplect = value;
                NeedComplectOptionModel.CurrentValue = value;
                RaisePropertyChanged(nameof(NeedComplect));
                RaisePropertyChanged(nameof(NeedComplectOptionModel));
            }
        }

        public CategoryOptionViewModel<bool> NeedComplectOptionModel { get; set; }

        public int NeedDescription
        {
            get
            {
                return Target.NeedDescription;
            }

            set
            {
                Target.NeedDescription = value;
                NeedDescriptionOptionModel.CurrentValue = value;
                RaisePropertyChanged(nameof(NeedDescription));
                RaisePropertyChanged(nameof(NeedDescriptionOptionModel));
            }
        }

        public CategoryOptionViewModel<int> NeedDescriptionOptionModel { get; set; }

        public int NeedPhoto
        {
            get
            {
                return Target.NeedPhoto;
            }

            set
            {
                Target.NeedPhoto = value;
                NeedPhotoOptionModel.CurrentValue = value;
                RaisePropertyChanged(nameof(NeedPhoto));
                RaisePropertyChanged(nameof(NeedPhotoOptionModel));
            }
        }

        public CategoryOptionViewModel<int> NeedPhotoOptionModel { get; set; }

        public string PrefixRus
        {
            get
            {
                return Target.PrefixRus;
            }

            set
            {
                Target.PrefixRus = value;
                PrefixRusOptionModel.CurrentValue = value;
                RaisePropertyChanged(nameof(PrefixRus));
                RaisePropertyChanged(nameof(PrefixRusOptionModel));
            }
        }

        public CategoryOptionViewModel<string> PrefixRusOptionModel { get; set; }

        public string PrefixUkr
        {
            get
            {
                return Target.PrefixUkr;
            }

            set
            {
                Target.PrefixUkr = value;
                PrefixUkrOptionModel.CurrentValue = value;
                RaisePropertyChanged(nameof(PrefixUkr));
                RaisePropertyChanged(nameof(PrefixUkrOptionModel));
            }
        }

        public CategoryOptionViewModel<string> PrefixUkrOptionModel { get; set; }

        public string PrefixEn
        {
            get
            {
                return Target.PrefixEn;
            }

            set
            {
                Target.PrefixEn = value;
                PrefixEnOptionModel.CurrentValue = value;
                RaisePropertyChanged(nameof(PrefixEn));
                RaisePropertyChanged(nameof(PrefixEnOptionModel));
            }
        }

        public CategoryOptionViewModel<string> PrefixEnOptionModel { get; set; }

        public int TagFormatId
        {
            get
            {
                return Target.TagFormatId;
            }

            set
            {
                Target.TagFormatId = value;
                TagFormatIdOptionModel.CurrentValue = value;
                RaisePropertyChanged(nameof(TagFormatId));
                RaisePropertyChanged(nameof(TagFormatIdOptionModel));
            }
        }

        public CategoryOptionViewModel<int> TagFormatIdOptionModel { get; set; }

        public int TaxRateId
        {
            get
            {
                return Target.TaxRateId;
            }

            set
            {
                Target.TaxRateId = value;
                TaxRateIdOptionModel.CurrentValue = value;
                RaisePropertyChanged(nameof(TaxRateId));
                RaisePropertyChanged(nameof(TaxRateIdOptionModel));
            }
        }

        public CategoryOptionViewModel<int> TaxRateIdOptionModel { get; set; }

        public int? TypeId
        {
            get
            {
                return Target.TypeId;
            }

            set
            {
                Target.TypeId = value;
                TypeIdOptionModel.CurrentValue = value;
                RaisePropertyChanged(nameof(TypeId));
                RaisePropertyChanged(nameof(TypeIdOptionModel));
            }
        }

        public CategoryOptionViewModel<int?> TypeIdOptionModel { get; set; }

        public int? AdmitadTariffCodeId
        {
            get
            {
                return Target.AdmitadTariffCodeId;
            }

            set
            {
                Target.AdmitadTariffCodeId = value;
                AdmitadTariffCodeOptionModel.CurrentValue = value;
                RaisePropertyChanged(nameof(AdmitadTariffCodeId));
                RaisePropertyChanged(nameof(AdmitadTariffCodeOptionModel));
            }
        }

        public CategoryOptionViewModel<int?> AdmitadTariffCodeOptionModel { get; set; }

        public int? SalesDoublerTariffCodeId
        {
            get
            {
                return Target.SalesDoublerTariffCodeId;
            }

            set
            {
                Target.SalesDoublerTariffCodeId = value;
                SalesDoublerTariffCodeOptionModel.CurrentValue = value;
                RaisePropertyChanged(nameof(SalesDoublerTariffCodeId));
                RaisePropertyChanged(nameof(SalesDoublerTariffCodeOptionModel));
            }
        }

        public CategoryOptionViewModel<int?> SalesDoublerTariffCodeOptionModel { get; set; }

        public int? ReferralDiscountCodeId
        {
            get { return Target.ReferralDiscountCodeId; }

            set
            {
                Target.ReferralDiscountCodeId = value;
                ReferralDiscountCodeOptionModel.CurrentValue = value;
                RaisePropertyChanged(nameof(ReferralDiscountCodeId));
                RaisePropertyChanged(nameof(ReferralDiscountCodeOptionModel));
            }
        }

        public CategoryOptionViewModel<int?> ReferralDiscountCodeOptionModel { get; set; }

        public int? SegmentLimit
        {
            get { return Target.SegmentLimit; }

            set
            {
                Target.SegmentLimit = value;
                SegmentLimitOptionModel.CurrentValue = value;
                RaisePropertyChanged(nameof(SegmentLimit));
                RaisePropertyChanged(nameof(SegmentLimitOptionModel));
            }
        }

        public CategoryOptionViewModel<int?> SegmentLimitOptionModel { get; set; }

        public int? SegmentFeaturesLimit
        {
            get { return Target.SegmentFeaturesLimit; }

            set
            {
                Target.SegmentFeaturesLimit = value;
                SegmentFeaturesLimitOptionModel.CurrentValue = value;
                RaisePropertyChanged(nameof(SegmentFeaturesLimit));
                RaisePropertyChanged(nameof(SegmentFeaturesLimitOptionModel));
            }
        }

        public CategoryOptionViewModel<int?> SegmentFeaturesLimitOptionModel { get; set; }

        public string NameTrans
        {
            get
            {
                return Target.NameTrans;
            }

            set
            {
                Target.NameTrans = value ?? string.Empty;
                NameTransOptionModel.CurrentValue = value ?? string.Empty;
                RaisePropertiesChanged(nameof(NameTrans), nameof(NameTransOptionModel));
            }
        }

        public CategoryOptionViewModel<string> NameTransOptionModel { get; set; }

        public string NameTransUkr
        {
            get
            {
                return Target.NameTransUkr;
            }

            set
            {
                Target.NameTransUkr = value ?? string.Empty;
                NameTransUkrOptionModel.CurrentValue = value ?? string.Empty;
                RaisePropertiesChanged(nameof(NameTransUkr), nameof(NameTransUkrOptionModel));
            }
        }

        public CategoryOptionViewModel<string> NameTransUkrOptionModel { get; set; }

        public string NameTransEn
        {
            get
            {
                return Target.NameTransEn;
            }

            set
            {
                Target.NameTransEn = value ?? string.Empty;
                NameTransEnOptionModel.CurrentValue = value ?? string.Empty;
                RaisePropertiesChanged(nameof(NameTransEn), nameof(NameTransEnOptionModel));
            }
        }

        public CategoryOptionViewModel<string> NameTransEnOptionModel { get; set; }

        public string NameBreadcrumbs
        {
            get
            {
                return Target.NameBreadcrumbs;
            }

            set
            {
                Target.NameBreadcrumbs = value ?? string.Empty;
                NameBreadcrumbsOptionModel.CurrentValue = value ?? string.Empty;
                RaisePropertiesChanged(nameof(NameBreadcrumbs), nameof(NameBreadcrumbsOptionModel));
            }
        }

        public CategoryOptionViewModel<string> NameBreadcrumbsOptionModel { get; set; }

        public string NameBreadcrumbsUkr
        {
            get
            {
                return Target.NameBreadcrumbsUkr;
            }

            set
            {
                Target.NameBreadcrumbsUkr = value ?? string.Empty;
                NameBreadcrumbsUkrOptionModel.CurrentValue = value ?? string.Empty;
                RaisePropertiesChanged(nameof(NameBreadcrumbsUkr), nameof(NameBreadcrumbsUkrOptionModel));
            }
        }

        public CategoryOptionViewModel<string> NameBreadcrumbsUkrOptionModel { get; set; }

        public string NameBreadcrumbsEn
        {
            get
            {
                return Target.NameBreadcrumbsEn;
            }

            set
            {
                Target.NameBreadcrumbsEn = value ?? string.Empty;
                NameBreadcrumbsEnOptionModel.CurrentValue = value ?? string.Empty;
                RaisePropertiesChanged(nameof(NameBreadcrumbsEn), nameof(NameBreadcrumbsEnOptionModel));
            }
        }

        public CategoryOptionViewModel<string> NameBreadcrumbsEnOptionModel { get; set; }

        public string Description
        {
            get
            {
                return Target.Description;
            }

            set
            {
                Target.Description = value ?? string.Empty;
                DescriptionOptionModel.CurrentValue = value ?? string.Empty;
                RaisePropertiesChanged(nameof(Description), nameof(DescriptionOptionModel));
            }
        }

        public CategoryOptionViewModel<string> DescriptionOptionModel { get; set; }

        public string DescriptionUkr
        {
            get
            {
                return Target.DescriptionUkr;
            }

            set
            {
                Target.DescriptionUkr = value ?? string.Empty;
                DescriptionUkrOptionModel.CurrentValue = value ?? string.Empty;
                RaisePropertiesChanged(nameof(DescriptionUkr), nameof(DescriptionUkrOptionModel));
            }
        }

        public CategoryOptionViewModel<string> DescriptionUkrOptionModel { get; set; }

        public string DescriptionEn
        {
            get
            {
                return Target.DescriptionEn;
            }

            set
            {
                Target.DescriptionEn = value ?? string.Empty;
                DescriptionEnOptionModel.CurrentValue = value ?? string.Empty;
                RaisePropertiesChanged(nameof(DescriptionEn), nameof(DescriptionEnOptionModel));
            }
        }

        public CategoryOptionViewModel<string> DescriptionEnOptionModel { get; set; }

        public string PromoInfo
        {
            get
            {
                return Target.PromoInfo;
            }

            set
            {
                Target.PromoInfo = value;
                PromoInfoOptionModel.CurrentValue = value;
                RaisePropertiesChanged(nameof(PromoInfo), nameof(PromoInfoOptionModel));
            }
        }

        public CategoryOptionViewModel<string> PromoInfoOptionModel { get; set; }

        public string PromoInfoUkr
        {
            get
            {
                return Target.PromoInfoUkr;
            }

            set
            {
                Target.PromoInfoUkr = value;
                PromoInfoUkrOptionModel.CurrentValue = value;
                RaisePropertiesChanged(nameof(PromoInfoUkr), nameof(PromoInfoUkrOptionModel));
            }
        }

        public CategoryOptionViewModel<string> PromoInfoUkrOptionModel { get; set; }

        public string PromoInfoEn
        {
            get
            {
                return Target.PromoInfoEn;
            }

            set
            {
                Target.PromoInfoEn = value;
                PromoInfoEnOptionModel.CurrentValue = value;
                RaisePropertiesChanged(nameof(PromoInfoEn), nameof(PromoInfoEnOptionModel));
            }
        }

        public CategoryOptionViewModel<string> PromoInfoEnOptionModel { get; set; }

        public int? CategoryPriority
        {
            get
            {
                return Target.CategoryPriority;
            }

            set
            {
                Target.CategoryPriority = value;
                CategoryPriorityOptionModel.CurrentValue = value;
                RaisePropertiesChanged(nameof(CategoryPriority), nameof(CategoryPriorityOptionModel));
            }
        }

        public CategoryOptionViewModel<int?> CategoryPriorityOptionModel { get; set; }

        public decimal? PlannedTurnover
        {
            get
            {
                return Target.PlannedTurnover;
            }

            set
            {
                Target.PlannedTurnover = value;
                PlannedTurnoverOptionModel.CurrentValue = value;
                RaisePropertiesChanged(nameof(PlannedTurnover), nameof(PlannedTurnoverOptionModel));
            }
        }

        public CategoryOptionViewModel<decimal?> PlannedTurnoverOptionModel { get; set; }

        public decimal? PlannedGrossProfit
        {
            get
            {
                return Target.PlannedGrossProfit;
            }

            set
            {
                Target.PlannedGrossProfit = value;
                PlannedGrossProfitOptionModel.CurrentValue = value;
                RaisePropertiesChanged(nameof(PlannedGrossProfit), nameof(PlannedGrossProfitOptionModel));
            }
        }

        public CategoryOptionViewModel<decimal?> PlannedGrossProfitOptionModel { get; set; }

        public decimal? PlannedProfit
        {
            get
            {
                return Target.PlannedProfit;
            }

            set
            {
                Target.PlannedProfit = value;
                PlannedProfitOptionModel.CurrentValue = value;
                RaisePropertiesChanged(nameof(PlannedProfit), nameof(PlannedProfitOptionModel));
            }
        }

        public CategoryOptionViewModel<decimal?> PlannedProfitOptionModel { get; set; }

        public decimal? PlannedQuantity
        {
            get
            {
                return Target.PlannedQuantity;
            }

            set
            {
                Target.PlannedQuantity = value;
                PlannedQuantityOptionModel.CurrentValue = value;
                RaisePropertiesChanged(nameof(PlannedQuantity), nameof(PlannedQuantityOptionModel));
            }
        }

        public CategoryOptionViewModel<decimal?> PlannedQuantityOptionModel { get; set; }

        public decimal? PurchasePercent
        {
            get
            {
                return Target.PurchasePercent;
            }

            set
            {
                Target.PurchasePercent = value;
                PurchasePercentOptionModel.CurrentValue = value;
                RaisePropertiesChanged(nameof(PurchasePercent), nameof(PurchasePercentOptionModel));
            }
        }

        public CategoryOptionViewModel<decimal?> PurchasePercentOptionModel { get; set; }

        public decimal? BountyPercent
        {
            get
            {
                return Target.BountyPercent;
            }

            set
            {
                Target.BountyPercent = value;
                BountyPercentOptionModel.CurrentValue = value;
                RaisePropertiesChanged(nameof(BountyPercent), nameof(BountyPercentOptionModel));
            }
        }

        public CategoryOptionViewModel<decimal?> BountyPercentOptionModel { get; set; }

        #region PrintMarkering

        public bool PrintMarkers
        {
            get
            {
                return Target.PrintMarkers;
            }

            set
            {
                Target.PrintMarkers = value;
                PrintMarkersOptionModel.CurrentValue = value;
                RaisePropertyChanged(nameof(PrintMarkers));
                RaisePropertyChanged(nameof(PrintMarkersOptionModel));
                RaisePropertyChanged(nameof(FeatureId));
                RaisePropertyChanged(nameof(MarkerManufacture));
                RaisePropertyChanged(nameof(MarkerManufactureAddress));
            }
        }

        public CategoryOptionViewModel<bool> PrintMarkersOptionModel { get; set; }

        public string MarkerManufacture
        {
            get
            {
                return Target.MarkerManufacture;
            }

            set
            {
                Target.MarkerManufacture = value;
                MarkerManufactureOptionModel.CurrentValue = value;
                RaisePropertiesChanged(nameof(MarkerManufacture), nameof(MarkerManufactureOptionModel));
            }
        }

        public CategoryOptionViewModel<string> MarkerManufactureOptionModel { get; set; }

        public string MarkerManufactureAddress
        {
            get
            {
                return Target.MarkerManufactureAddress;
            }

            set
            {
                Target.MarkerManufactureAddress = value;
                MarkerManufactureAddressOptionModel.CurrentValue = value;
                RaisePropertiesChanged(nameof(MarkerManufactureAddress), nameof(MarkerManufactureAddressOptionModel));
            }
        }

        public CategoryOptionViewModel<string> MarkerManufactureAddressOptionModel { get; set; }

        public int? FeatureId
        {
            get
            {
                return Target.FeatureId;
            }

            set
            {
                Target.FeatureId = value;
                FeatureIdOptionModel.CurrentValue = value;
                RaisePropertyChanged(nameof(FeatureId));
                RaisePropertyChanged(nameof(FeatureIdOptionModel));
            }
        }

        public CategoryOptionViewModel<int?> FeatureIdOptionModel { get; set; }

        #endregion

        public string Error => string.Empty;

        private IWebClient WebClient { get; }

        private IDictionaries Dictionaries { get; }

        public string this[string columnName] => IDataErrorInfoHelper.GetErrorText(this, columnName);

        public static CategoryOptionsViewModel GetEmptyViewModel()
        {
            return new CategoryOptionsViewModel
            {
                Category = new CategoryFullDto { Level = 1 },
                Parent = new CategoryFullDto { Level = 1 },
                Target = new CategoryFullDto { Level = 1 }
            };
        }

        public IReadOnlyCollection<ICategoryOption> GetChangedOptions()
        {
            IEnumerable<ICategoryOption> q = from pi in GetCategoryOptionsPropertyInfos()
                    where pi.CanRead && pi.CanWrite
                    let optionObject = pi.GetValue(this)
                    let oldValue = pi.PropertyType.GetProperty(nameof(NamesSource.InitialValue)).GetValue(optionObject)
                    let newValue = pi.PropertyType.GetProperty(nameof(NamesSource.CurrentValue)).GetValue(optionObject)
                    let overrideOption = (CategoryOverrideOption)pi.PropertyType.GetProperty(nameof(NamesSource.OverrideOption)).GetValue(optionObject)
                    where !oldValue.Same(newValue)
                    select optionObject as ICategoryOption;

            List<ICategoryOption> changedOptions = q.ToList();

            return changedOptions;
        }

        public IReadOnlyCollection<CategoryOptionSaveDto> GetChangedOptionsSaveDtos()
        {
            CategoryOptionSaveDto[] dtos;

            if (Target != null && Category != null)
            {
                var changedOptions = GetChangedOptions();

                dtos = changedOptions
                    .Select(x => new CategoryOptionSaveDto(GetPropertyNameByOptionName(x.OptionName), x.CurrentValue, (CategoryOverrideOptions)x.OverrideOption.Id))
                    .ToArray();

                IsChanged = dtos.Any();
            }
            else
            {
                dtos = Array.Empty<CategoryOptionSaveDto>();
                IsChanged = false;
            }

            return dtos;
        }

        public void SaveDiff(IEnumerable<CategoryOptionSaveDto> optionSaveDtos, string newNameFull, string newNameFullUkr, string newNameFullEn)
        {
            // separate update because we cannot add properties to mappings
            NameFull = newNameFull;
            NameFullUkr = newNameFullUkr;
            NameFullEn = newNameFullEn;
            RaisePropertiesChanged(nameof(NameFull), nameof(NameFullUkr), nameof(NameFullEn));

            foreach (CategoryOptionSaveDto categoryOptionSaveDto in optionSaveDtos)
            {
                string propertyName = GetOptionNameByPropertyName(categoryOptionSaveDto.PropertyName);

                typeof(CategoryFullDto)
                        .GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)
                        .GetSetMethod()
                        .Invoke(Category, new[] { categoryOptionSaveDto.PropertyValueNew });

                RaisePropertyChanged(propertyName);
                RaisePropertyChanged($"{propertyName}{CategoryOptionModelEnding}");
            }

            foreach (PropertyInfo categoryOption in GetType().GetProperties().Where(x => x.PropertyType.GetInterface(nameof(ICategoryOption)) != null))
            {
                object propertyObject = categoryOption.GetValue(this);

                categoryOption.PropertyType
                    .GetMethod(nameof(NameOptionModel.InitialValueChanged))
                    .Invoke(propertyObject, null);
            }
        }

        public async Task LoadOptionsDataAsync(int categoryId, ICategoryOptionsInitializer initializer, bool useCache = true)
        {
            Category = await WebClient.ExecuteApiRequestAsync(new QueryCategoryFull(categoryId));
            Parent = await WebClient.ExecuteApiRequestAsync(new QueryCategoryFull(Category.ParentId));

            mappings = await WebClient.ExecuteApiRequestAsync(new QueryCategoryMappings(), true);

            Target = Category.Clone();

            await Task.WhenAll(
                LoadEmployeesAsync(useCache, Category.EmployeeId, Category.EmployeeSupId),
                LoadWarrantyTypesAsync(),
                LoadPriceRobotModesAsync(),
                LoadKindsAsync(),
                LoadCurrenciesAsync(),
                LoadUsdCurrenciesAsync(),
                LoadTagFormatsAsync(useCache),
                LoadAdTariffCodesAsync(),
                LoadFeaturesAsync(Id));

            LoadTaxRates();
            LoadWarranties();
            LoadTypes();
            LoadReferralDiscountCodes();

            initializer.InitializeCategoryOptions(this);

            CanChangeAnyField = GetCategoryOptionsPropertyInfos()
                .Select(pi => new { PropertyInfo = pi, PropertyValue = pi.GetValue(this) })
                .Select(x => (bool)x.PropertyInfo.PropertyType.GetProperty(nameof(NamesSource.IsEditable)).GetValue(x.PropertyValue))
                .Any(y => y);
        }

        public void ResetAllChanges()
        {
            MethodInfo mi = typeof(ICategoryOption).GetMethod(nameof(ICategoryOption.ResetChanges));

            foreach (PropertyInfo pi in GetCategoryOptionsPropertyInfos())
            {
                object obj = pi.GetValue(this);
                mi.Invoke(obj, null);
            }
        }

        public void NotifyPropertyChanged(string propertyName)
        {
            RaisePropertyChanged(propertyName);
        }

        private IEnumerable<PropertyInfo> GetCategoryOptionsPropertyInfos()
        {
            return GetType()
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(x => x.PropertyType.GetInterface(nameof(ICategoryOption)) != null);
        }

        private async Task LoadTagFormatsAsync(bool useCache)
        {
            List<TagFormatDto> tagFormats = await WebClient.ExecuteApiRequestAsync(new QueryTagFromats(), useCache).GetPagedResultDataAsync();
            TagFormats.Clear();
            TagFormats.AddRange(tagFormats);
        }

        private void LoadTaxRates()
        {
            TaxRates.Clear();
            TaxRates.AddRange(Dictionaries.GetItems<TaxRate>());
        }

        private void LoadWarranties()
        {
            Warranties.Clear();
            Warranties.AddRange(Dictionaries.GetItems<Warranty>());
        }

        private void LoadTypes()
        {
            Types.Clear();
            Types.AddRange(Dictionaries.GetItems<CategoryType>().Where(x => x.Id > 0));
        }

        private void LoadReferralDiscountCodes()
        {
            ReferralDiscountCodes.Clear();
            ReferralDiscountCodes.AddRange(Dictionaries.GetItems<ReferralDiscountCode>());
        }

        private async Task LoadAdTariffCodesAsync()
        {
            AdmitadTariffCodes.Clear();
            SalesDoublerTariffCodes.Clear();

            List<AdProviderTariffCodeDto> adTariffCodes = await WebClient.ExecuteApiRequestAsync(new QueryAdProvidersTariffCodes(), true);

            AdmitadTariffCodes.AddRange(adTariffCodes.Where(x => x.AdProviderId == AdProviderTypeIds.Admitad));
            SalesDoublerTariffCodes.AddRange(adTariffCodes.Where(x => x.AdProviderId == AdProviderTypeIds.SalesDoubler));
        }

        private Task LoadUsdCurrenciesAsync()
        {
            UsdCurrencies.Clear();
            UsdCurrencies.AddRange(GetUsdCurrencies());

            return Task.CompletedTask;

            IEnumerable<UsdCurrency> GetUsdCurrencies()
            {
                yield return Client.Dictionaries.UsdCurrency.Plus;
                yield return Client.Dictionaries.UsdCurrency.Minus;
            }
        }

        private Task LoadCurrenciesAsync()
        {
            Currencies.Clear();
            Currencies.AddRange(GetCurrencies());

            return Task.CompletedTask;

            IEnumerable<Currency> GetCurrencies()
            {
                yield return Telemart.Client.Dictionaries.Currency.Uah;
                yield return Telemart.Client.Dictionaries.Currency.Usd;
                yield return Telemart.Client.Dictionaries.Currency.Eur;
            }
        }

        private Task LoadKindsAsync()
        {
            Kinds.Clear();
            Kinds.AddRange(GetKinds());

            return Task.CompletedTask;

            IEnumerable<ProductKind> GetKinds()
            {
                yield return ProductKind.New;
                yield return ProductKind.No;
                yield return ProductKind.Ref;
                yield return ProductKind.Service;
            }
        }

        private Task LoadPriceRobotModesAsync()
        {
            PriceRobotAutoModes.Clear();
            PriceRobotManualModes.Clear();

            PriceRobotAutoModes.AddRange(GetItems());
            PriceRobotManualModes.AddRange(GetItems());

            return Task.CompletedTask;

            IEnumerable<PriceRobotMode> GetItems()
            {
                yield return PriceRobotMode.No;
                yield return PriceRobotMode.AvailMinus;
                yield return PriceRobotMode.AvailPlus;
                yield return PriceRobotMode.Price;
                yield return PriceRobotMode.OnlyParameters;
            }
        }

        private async Task LoadEmployeesAsync(bool useCache, int employeeId, int employeeSupId)
        {
            List<EmployeeDto> employees = await WebClient.ExecuteApiRequestAsync(new QueryEmployees(), useCache).GetPagedResultDataAsync();

            IEnumerable<EmployeeViewItem> items = employees
                .Where(x => x.Active || x.Id == employeeId || x.Id == employeeSupId)
                .OrderBy(x => x.Name)
                .Select(x => new EmployeeViewItem(x.Id, x.Name));

            Employees.Clear();
            Employees.AddRange(items);
        }

        private Task LoadWarrantyTypesAsync()
        {
            WarrantyTypes.Clear();
            WarrantyTypes.AddRange(Dictionaries.GetItems<WarrantyType>().ToObservableRangeCollection());
            return Task.CompletedTask;
        }

        private async Task LoadFeaturesAsync(int categoryId)
        {
            PagedResult<FeatureGroupDto> featureGroups = await WebClient.ExecuteApiRequestAsync(new QueryFeatureGroups(categoryId, true));

            ReadOnlyObservableCollection<FeatureDto> features = featureGroups.Data?.SelectMany(x => x.Features).ToReadOnlyObservableCollection();

            Features.AddRange(features?.OrderBy(x => x.Name).ToObservableRangeCollection());
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e != null && !string.Equals(e.PropertyName, nameof(IsChanged), StringComparison.Ordinal))
            {
                RaisePropertyChanged(nameof(IsChanged));
            }
        }

        private string GetPropertyNameByOptionName(string optionName)
        {
            return mappings.First(y => y.Item1.Equals(optionName, StringComparison.Ordinal)).Item2;
        }

        private string GetOptionNameByPropertyName(string propertyName)
        {
            return mappings.First(y => y.Item2.Equals(propertyName, StringComparison.Ordinal)).Item1;
        }
    }
}