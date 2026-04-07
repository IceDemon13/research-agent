using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Common.Validation;
using Telemart.Client.Properties;

namespace Telemart.Client.ViewModels.Directories.Category
{
    internal sealed class CategoryOptionsViewModelMetadata
    {
        private const string Seo = "SEO";
        private const string Logistics = "Логистика";
        private const string ProductLabeling = "Маркировка товара";

        private static readonly string Content = Resources.CategoryEditingData_Content;
        private static readonly string Main = Resources.CategoryEditingData_Main;
        private static readonly string Prefix = Resources.CategoryEditingData_Prefix;
        private static readonly string Properties = Resources.CategoryEditingData_Properties;
        private static readonly string Bookkeeping = Resources.CategoryEditingData_Bookkeeping;
        private static readonly string Robot = Resources.CategoryEditingData_Robot;
        private static readonly string TradingPlaces = Resources.CategoryEditingData_TradingPlaces;

        public static void BuildMetadata(MetadataBuilder<CategoryOptionsViewModel> builder)
        {
            BuildDisplayMetadata(builder);
            BuildLayoutMetadata(builder);
            BuildHiddenMetadata(builder);
        }

        private static void BuildDisplayMetadata(MetadataBuilder<CategoryOptionsViewModel> builder)
        {
            builder.Property(x => x.Id).DisplayName(Resources.CategoryEditingData_Main_CategoryId);
            builder.Property(x => x.ParentId).DisplayName(Resources.CategoryEditingData_Main_ParentId);
            builder.Property(x => x.Category1C).DisplayName(Resources.CategoryEditingData_Main_Category1C);
            builder.Property(x => x.Name).DisplayName(Resources.CategoryEditingData_Main_Name).ApplyCategoryNameValidationRules();
            builder.Property(x => x.NameUkr).DisplayName(Resources.CategoryEditingData_Main_NameUkr).ApplyCategoryNameUkrValidationRules();
            builder.Property(x => x.NameEn).DisplayName(Resources.CategoryEditingData_Main_NameEn).ApplyCategoryNameEnValidationRules();
            builder.Property(x => x.NameFull).DisplayName("Полное название (рус.)");
            builder.Property(x => x.NameFullUkr).DisplayName("Полное название (укр.)");
            builder.Property(x => x.NameFullEn).DisplayName("Полное название (англ.)");
            builder.Property(x => x.Manufactor).DisplayName(Resources.CategoryEditingData_Main_Manufactor);
            builder.Property(x => x.IsParent).DisplayName(Resources.CategoryEditingData_Main_IsParent);
            builder.Property(x => x.Active).DisplayName(Resources.CategoryEditingData_Main_Active);
            builder.Property(x => x.UseInTradeIn).DisplayName(Resources.CategoryEditingData_Main_Use_In_Trade_In);
            builder.Property(x => x.TradeInServiceCost).DisplayName(Resources.CategoryEditingData_Main_Trade_In_Cost);
            builder.Property(x => x.TradeInServiceCostCurrencyId).DisplayName(Resources.CategoryEditingData_Main_Trade_In_Cost_Currency);
            builder.Property(x => x.MinCategoryMarginPercent).DisplayName("Минимальная маржа по категории, %").MatchesRule(x => x is null or > 0 and < 100, () => "Значение должно быть больше 0 и меньше 100");

            builder.Property(x => x.RobotScript).DisplayName(Resources.CategoryEditingData_Robot_RobotScript);
            builder.Property(x => x.RobotScriptParameters).DisplayName(Resources.CategoryEditingData_Robot_RobotScriptParameters);
            builder.Property(x => x.RobotModeManualId).DisplayName(Resources.CategoryEditingData_Robot_RobotModeManual);
            builder.Property(x => x.RobotModeAutoId).DisplayName(Resources.CategoryEditingData_Robot_RobotModeAuto);

            builder.Property(x => x.Hotline).DisplayName(Resources.CategoryEditingData_TradingPlaces_Hotline);

            builder.Property(x => x.EmployeeId).DisplayName(Resources.CategoryEditingData_Properties_EmployeeId);
            builder.Property(x => x.EmployeeSupId).DisplayName("Закупает");
            builder.Property(x => x.WarrantyTypeId).DisplayName("Тип гарантии");
            builder.Property(x => x.Kind).DisplayName(Resources.CategoryEditingData_Properties_Kind);
            builder.Property(x => x.Currency).DisplayName(Resources.CategoryEditingData_Properties_Currency);
            builder.Property(x => x.UsdCurrency).DisplayName(Resources.CategoryEditingData_Properties_UsdCurrency);
            builder.Property(x => x.ShortNamesFor1C).DisplayName(Resources.CategoryEditingData_Properties_ShortName1c);
            builder.Property(x => x.PlannedMarkup).DisplayName("Плановая рентабельность, %");
            builder.Property(x => x.UseNewRobot).DisplayName("Использовать новый робот");
            builder.Property(x => x.UsePlannedMarkupInAutoShowcase).DisplayName("Использовать рентабельность в автовитрине");
            builder.Property(x => x.ShowcaseSkuLimit).DisplayName("Лимит одного sku на витрине");
            builder.Property(x => x.WarrantyRetailId).DisplayName(Resources.CategoryEditingData_Properties_WarrantyRetail);
            builder.Property(x => x.WarrantyWholesaleId).DisplayName(Resources.CategoryEditingData_Properties_WarrantyWholesale);
            builder.Property(x => x.TagFormatId).DisplayName(Resources.CategoryEditingData_TagFormat);
            builder.Property(x => x.TaxRateId).DisplayName(Resources.CategoryEditingData_TaxRate);
            builder.Property(x => x.TypeId).DisplayName(Resources.CategoryEditingData_Type);
            builder.Property(x => x.AdmitadTariffCodeId).DisplayName(Resources.CategoryEditingData_AdmitadTariffCode);
            builder.Property(x => x.SalesDoublerTariffCodeId).DisplayName(Resources.CategoryEditingData_SalesDoublerTariffCode);
            builder.Property(x => x.ReferralDiscountCodeId).DisplayName("Тариф реферальной программы");
            builder.Property(x => x.SegmentLimit).DisplayName("Лимит сегментов").MatchesInstanceRule((x, y) => x is null or > 0, () => "Значение должно быть больше 0");
            builder.Property(x => x.SegmentFeaturesLimit).DisplayName("Лимит сегм. хар-ик").MatchesInstanceRule((x, y) => x is null or > 0, () => "Значение должно быть больше 0");

            builder.Property(x => x.PrintWarrantyCard).DisplayName(Resources.CategoryEditingData_Properties_PrintWarrantyCard);
            builder.Property(x => x.KeepSerial).DisplayName(Resources.CategoryEditingData_Properties_KeepSerial);
            builder.Property(x => x.KeepPn).DisplayName(Resources.CategoryEditingData_Properties_KeepPn);
            builder.Property(x => x.SelfBarcode).DisplayName(Resources.CategoryEditingData_Properties_SelfBarcode);
            builder.Property(x => x.KeepDimensions).DisplayName(Resources.CategoryEditingData_Properties_KeepDimensions);
            builder.Property(x => x.StickerFragile).DisplayName(Resources.CategoryEditingData_Properties_StickerFragile);
            builder.Property(x => x.StickerThisWayUp).DisplayName(Resources.CategoryEditingData_Properties_StickerThisWayUp);
            builder.Property(x => x.WeightEstimated).DisplayName(Resources.CategoryEditingData_Properties_Weight);
            builder.Property(x => x.FreeDelivery).DisplayName(Resources.CategoryEditingData_Properties_FreeDelivery);

            builder.Property(x => x.YandexMarketHid).DisplayName(Resources.CategoryEditingData_Main_YandexMarketHid);
            builder.Property(x => x.DaysToFill).DisplayName(Resources.CategoryEditingData_Content_DaysToFill);
            builder.Property(x => x.NeedContent).DisplayName(Resources.CategoryEditingData_Content_NeedContent);
            builder.Property(x => x.NeedVideo).DisplayName(Resources.CategoryEditingData_Content_NeedVideo);
            builder.Property(x => x.NeedComplect).DisplayName(Resources.CategoryEditingData_Content__NeedComplect);
            builder.Property(x => x.NeedDescription).DisplayName(Resources.CategoryEditingData_Content_NeedDescription);
            builder.Property(x => x.NeedPhoto).DisplayName(Resources.CategoryEditingData_Content_NeedPhoto);

            builder.Property(x => x.PrefixRus).DisplayName(Resources.CategoryEditingData_Prefix_PrefixRus);
            builder.Property(x => x.PrefixUkr).DisplayName(Resources.CategoryEditingData_Prefix_PrefixUkr);
            builder.Property(x => x.PrefixEn).DisplayName(Resources.CategoryEditingData_Prefix_PrefixEn);

            builder.Property(x => x.NameTrans).DisplayName("Название транслит (рус.)");
            builder.Property(x => x.NameTransUkr).DisplayName("Название транслит (укр.)");
            builder.Property(x => x.NameTransEn).DisplayName("Название транслит (англ.)");
            builder.Property(x => x.NameBreadcrumbs).DisplayName("Назв. в хлеб. крошках (рус.)");
            builder.Property(x => x.NameBreadcrumbsUkr).DisplayName("Назв. в хлеб. крошках (укр.)");
            builder.Property(x => x.NameBreadcrumbsEn).DisplayName("Назв. в хлеб. крошках (англ.)");
            builder.Property(x => x.Description).DisplayName("Описание (рус.)");
            builder.Property(x => x.DescriptionUkr).DisplayName("Описание (укр.)");
            builder.Property(x => x.DescriptionEn).DisplayName("Описание (англ.)");
            builder.Property(x => x.PromoInfo).DisplayName("Промо текст (рус.)");
            builder.Property(x => x.PromoInfoUkr).DisplayName("Промо текст (укр.)");
            builder.Property(x => x.PromoInfoEn).DisplayName("Промо текст (англ.)");

            builder.Property(x => x.CategoryPriority).DisplayName("Приоритет категории");
            builder.Property(x => x.PlannedTurnover).DisplayName("Плановый оборот");
            builder.Property(x => x.PlannedGrossProfit).DisplayName("Плановая валовая прибыль");
            builder.Property(x => x.PlannedProfit).DisplayName("Плановая рентабельность");
            builder.Property(x => x.PlannedQuantity).DisplayName("Плановое количество");
            builder.Property(x => x.PurchasePercent).DisplayName("Товары под закупку, %").MatchesRule(x => x == null || x <= 100, () => "Значение должно быть меньше либо равно 100");
            builder.Property(x => x.BountyPercent).DisplayName("Премия, %");

            builder.Property(x => x.PrintMarkers).DisplayName("Печатать етикетки");
            builder.Property(x => x.MarkerManufacture).DisplayName("Название компании");
            builder.Property(x => x.MarkerManufactureAddress).DisplayName("Адрес");
            builder.Property(x => x.FeatureId).DisplayName("Характеристика");
        }

        private static void BuildHiddenMetadata(MetadataBuilder<CategoryOptionsViewModel> builder)
        {
            builder.Property(x => x.ParentId).Hidden();
            builder.Property(x => x.IsChanged).Hidden();
            builder.Property(x => x.Parent).Hidden();
            builder.Property(x => x.Category).Hidden();
            builder.Property(x => x.Target).Hidden();

            builder.Property(x => x.ActiveOptionModel).Hidden();
            builder.Property(x => x.UseInTradeInOptionModel).Hidden();
            builder.Property(x => x.TradeInServiceCostOptionModel).Hidden();
            builder.Property(x => x.TradeInServiceCostCurrencyOptionModel).Hidden();
            builder.Property(x => x.MinCategoryMarginPercentModel).Hidden();
            builder.Property(x => x.NameOptionModel).Hidden();
            builder.Property(x => x.NameUkrOptionModel).Hidden();
            builder.Property(x => x.NameEnOptionModel).Hidden();
            builder.Property(x => x.YandexMarketHidOptionModel).Hidden();
            builder.Property(x => x.ManufactorOptionModel).Hidden();

            builder.Property(x => x.RobotScriptOptionModel).Hidden();
            builder.Property(x => x.RobotScriptParametersOptionModel).Hidden();
            builder.Property(x => x.RobotModeManualIdOptionModel).Hidden();
            builder.Property(x => x.RobotModeAutoIdOptionModel).Hidden();

            builder.Property(x => x.HotlineOptionModel).Hidden();

            builder.Property(x => x.EmployeeIdOptionModel).Hidden();
            builder.Property(x => x.EmployeeSupIdOptionModel).Hidden();
            builder.Property(x => x.WarrantyTypeIdOptionModel).Hidden();
            builder.Property(x => x.KindOptionModel).Hidden();
            builder.Property(x => x.CurrencyOptionModel).Hidden();
            builder.Property(x => x.UsdCurrencyOptionModel).Hidden();
            builder.Property(x => x.ShortNamesFor1CModel).Hidden();
            builder.Property(x => x.PlannedMarkupModel).Hidden();
            builder.Property(x => x.UseNewRobotModel).Hidden();
            builder.Property(x => x.UsePlannedMarkupInAutoShowcaseModel).Hidden();
            builder.Property(x => x.ShowcaseSkuLimitModel).Hidden();
            builder.Property(x => x.WarrantyRetailIdOptionModel).Hidden();
            builder.Property(x => x.WarrantyWholesaleIdOptionModel).Hidden();
            builder.Property(x => x.TagFormatIdOptionModel).Hidden();
            builder.Property(x => x.TaxRateIdOptionModel).Hidden();
            builder.Property(x => x.TypeIdOptionModel).Hidden();
            builder.Property(x => x.AdmitadTariffCodeOptionModel).Hidden();
            builder.Property(x => x.SalesDoublerTariffCodeOptionModel).Hidden();
            builder.Property(x => x.ReferralDiscountCodeOptionModel).Hidden();
            builder.Property(x => x.SegmentLimitOptionModel).Hidden();
            builder.Property(x => x.SegmentFeaturesLimitOptionModel).Hidden();

            builder.Property(x => x.PrintWarrantyCardOptionModel).Hidden();
            builder.Property(x => x.KeepSerialOptionModel).Hidden();
            builder.Property(x => x.KeepPnOptionModel).Hidden();
            builder.Property(x => x.WeightEstimatedOptionModel).Hidden();
            builder.Property(x => x.FreeDeliveryOptionModel).Hidden();
            builder.Property(x => x.SelfBarcodeOptionModel).Hidden();
            builder.Property(x => x.KeepDimensionsOptionModel).Hidden();
            builder.Property(x => x.StickerFragileOptionModel).Hidden();
            builder.Property(x => x.StickerThisWayUpOptionModel).Hidden();

            builder.Property(x => x.DaysToFillOptionModel).Hidden();
            builder.Property(x => x.NeedContentOptionModel).Hidden();
            builder.Property(x => x.NeedVideoOptionModel).Hidden();
            builder.Property(x => x.NeedComplectOptionModel).Hidden();
            builder.Property(x => x.NeedDescriptionOptionModel).Hidden();
            builder.Property(x => x.NeedPhotoOptionModel).Hidden();

            builder.Property(x => x.PrefixRusOptionModel).Hidden();
            builder.Property(x => x.PrefixUkrOptionModel).Hidden();
            builder.Property(x => x.PrefixEnOptionModel).Hidden();

            builder.Property(x => x.NameTransOptionModel).Hidden();
            builder.Property(x => x.NameTransUkrOptionModel).Hidden();
            builder.Property(x => x.NameTransEnOptionModel).Hidden();
            builder.Property(x => x.NameBreadcrumbsOptionModel).Hidden();
            builder.Property(x => x.NameBreadcrumbsUkrOptionModel).Hidden();
            builder.Property(x => x.NameBreadcrumbsEnOptionModel).Hidden();
            builder.Property(x => x.DescriptionOptionModel).Hidden();
            builder.Property(x => x.DescriptionUkrOptionModel).Hidden();
            builder.Property(x => x.DescriptionEnOptionModel).Hidden();
            builder.Property(x => x.PromoInfoOptionModel).Hidden();
            builder.Property(x => x.PromoInfoUkrOptionModel).Hidden();
            builder.Property(x => x.PromoInfoEnOptionModel).Hidden();

            builder.Property(x => x.CategoryPriorityOptionModel).Hidden();
            builder.Property(x => x.PlannedTurnoverOptionModel).Hidden();
            builder.Property(x => x.PlannedGrossProfitOptionModel).Hidden();
            builder.Property(x => x.PlannedProfitOptionModel).Hidden();
            builder.Property(x => x.PlannedQuantityOptionModel).Hidden();
            builder.Property(x => x.PurchasePercentOptionModel).Hidden();
            builder.Property(x => x.BountyPercentOptionModel).Hidden();

            builder.Property(x => x.Employees).Hidden();
            builder.Property(x => x.WarrantyTypes).Hidden();
            builder.Property(x => x.Currencies).Hidden();
            builder.Property(x => x.UsdCurrencies).Hidden();
            builder.Property(x => x.PriceRobotManualModes).Hidden();
            builder.Property(x => x.PriceRobotAutoModes).Hidden();
            builder.Property(x => x.Kinds).Hidden();
            builder.Property(x => x.Warranties).Hidden();
            builder.Property(x => x.TagFormats).Hidden();
            builder.Property(x => x.TaxRates).Hidden();
            builder.Property(x => x.Types).Hidden();
            builder.Property(x => x.AdmitadTariffCodes).Hidden();
            builder.Property(x => x.SalesDoublerTariffCodes).Hidden();
            builder.Property(x => x.ReferralDiscountCodes).Hidden();

            builder.Property(x => x.Error).Hidden();
            builder.Property(x => x.ActiveValues).Hidden();
            builder.Property(x => x.IsEditingAllowed).Hidden();
            builder.Property(x => x.IsOptionsLockedByCurrentEmployee).Hidden();
            builder.Property(x => x.IsLongOperationInProgress).Hidden();
            builder.Property(x => x.CanChangeAnyField).Hidden();
            builder.Property(x => x.PrintMarkersOptionModel).Hidden();
            builder.Property(x => x.MarkerManufactureOptionModel).Hidden();
            builder.Property(x => x.MarkerManufactureAddressOptionModel).Hidden();
            builder.Property(x => x.FeatureIdOptionModel).Hidden();
            builder.Property(x => x.Features).Hidden();
        }

        private static void BuildLayoutMetadata(MetadataBuilder<CategoryOptionsViewModel> builder)
        {
            builder.Group(Main)
                .ContainsProperty(x => x.Id)
                .ContainsProperty(x => x.Category1C)
                .ContainsProperty(x => x.Name)
                .ContainsProperty(x => x.NameUkr)
                .ContainsProperty(x => x.NameEn)
                .ContainsProperty(x => x.NameFull)
                .ContainsProperty(x => x.NameFullUkr)
                .ContainsProperty(x => x.NameFullEn)
                .ContainsProperty(x => x.Manufactor)
                .ContainsProperty(x => x.IsParent)
                .ContainsProperty(x => x.Active)
                .ContainsProperty(x => x.UseInTradeIn)
                .ContainsProperty(x => x.TradeInServiceCost)
                .ContainsProperty(x => x.TradeInServiceCostCurrencyId)
                .ContainsProperty(x => x.MinCategoryMarginPercent);

            builder.Group(Properties)
                .ContainsProperty(x => x.EmployeeId)
                .ContainsProperty(x => x.EmployeeSupId)
                .ContainsProperty(x => x.WarrantyTypeId)
                .ContainsProperty(x => x.Currency)
                .ContainsProperty(x => x.WarrantyRetailId)
                .ContainsProperty(x => x.WarrantyWholesaleId)
                .ContainsProperty(x => x.TagFormatId)
                .ContainsProperty(x => x.TypeId)
                .ContainsProperty(x => x.AdmitadTariffCodeId)
                .ContainsProperty(x => x.SalesDoublerTariffCodeId)
                .ContainsProperty(x => x.ReferralDiscountCodeId)
                .ContainsProperty(x => x.SegmentLimit)
                .ContainsProperty(x => x.SegmentFeaturesLimit)
                .ContainsProperty(x => x.PlannedMarkup)
                .ContainsProperty(x => x.UsePlannedMarkupInAutoShowcase)
                .ContainsProperty(x => x.ShowcaseSkuLimit);

            builder.Group(Bookkeeping)
                .ContainsProperty(x => x.Kind)
                .ContainsProperty(x => x.Currency)
                .ContainsProperty(x => x.UsdCurrency)
                .ContainsProperty(x => x.TaxRateId)
                .ContainsProperty(x => x.ShortNamesFor1C);

            builder.Group("KPI")
               .ContainsProperty(x => x.CategoryPriority)
               .ContainsProperty(x => x.PlannedTurnover)
               .ContainsProperty(x => x.PlannedGrossProfit)
               .ContainsProperty(x => x.PlannedProfit)
               .ContainsProperty(x => x.PlannedQuantity)
               .ContainsProperty(x => x.PurchasePercent)
               .ContainsProperty(x => x.BountyPercent);

            builder.Group(Robot)
                .ContainsProperty(x => x.RobotScript)
                .ContainsProperty(x => x.UseNewRobot)
                .ContainsProperty(x => x.RobotScriptParameters)
                .ContainsProperty(x => x.RobotModeManualId)
                .ContainsProperty(x => x.RobotModeAutoId);

            builder.Group(TradingPlaces)
                .ContainsProperty(x => x.Hotline);

            builder.Group(Logistics)
                .ContainsProperty(x => x.PrintWarrantyCard)
                .ContainsProperty(x => x.KeepSerial)
                .ContainsProperty(x => x.KeepPn)
                .ContainsProperty(x => x.SelfBarcode)
                .ContainsProperty(x => x.KeepDimensions)
                .ContainsProperty(x => x.WeightEstimated)
                .ContainsProperty(x => x.FreeDelivery)
                .ContainsProperty(x => x.StickerFragile)
                .ContainsProperty(x => x.StickerThisWayUp);

            builder.Group(Content)
                .ContainsProperty(x => x.YandexMarketHid)
                .ContainsProperty(x => x.DaysToFill)
                .ContainsProperty(x => x.NeedContent)
                .ContainsProperty(x => x.NeedVideo)
                .ContainsProperty(x => x.NeedComplect)
                .ContainsProperty(x => x.NeedDescription)
                .ContainsProperty(x => x.NeedPhoto);

            builder.Group(Prefix)
                .ContainsProperty(x => x.PrefixRus)
                .ContainsProperty(x => x.PrefixUkr)
                .ContainsProperty(x => x.PrefixEn);

            builder.Group(Seo)
                .ContainsProperty(x => x.NameTrans)
                .ContainsProperty(x => x.NameTransUkr)
                .ContainsProperty(x => x.NameTransEn)
                .ContainsProperty(x => x.NameBreadcrumbs)
                .ContainsProperty(x => x.NameBreadcrumbsUkr)
                .ContainsProperty(x => x.NameBreadcrumbsEn)
                .ContainsProperty(x => x.Description)
                .ContainsProperty(x => x.DescriptionUkr)
                .ContainsProperty(x => x.DescriptionEn)
                .ContainsProperty(x => x.PromoInfo)
                .ContainsProperty(x => x.PromoInfoUkr)
                .ContainsProperty(x => x.PromoInfoEn);

            builder.Group(ProductLabeling)
                .ContainsProperty(x => x.PrintMarkers)
                .ContainsProperty(x => x.MarkerManufacture)
                .ContainsProperty(x => x.MarkerManufactureAddress)
                .ContainsProperty(x => x.FeatureId);
        }
    }
}