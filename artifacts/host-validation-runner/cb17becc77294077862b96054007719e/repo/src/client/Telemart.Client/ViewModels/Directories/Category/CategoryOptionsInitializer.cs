using System;
using System.Linq;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.ViewModels.Directories.Category
{
    public sealed class CategoryOptionsInitializer : ICategoryOptionsInitializer
    {
        public CategoryOptionsInitializer(IWebClient webClient)
        {
            WebClient = webClient ?? throw new ArgumentNullException(nameof(webClient));
        }

        private IWebClient WebClient { get; }

        private EmployeeDto CurrentEmployee => WebClient.AuthenticatedEmployee;

        public void InitializeCategoryOptions(CategoryOptionsViewModel categoryOptions)
        {
            categoryOptions.NameOptionModel = new CategoryOptionViewModel<string>(
                categoryOptions.Parent,
                categoryOptions.Target,
                categoryOptions.Category,
                x => x.Name,
                (x, y) =>
                {
                    x.Name = y;
                    categoryOptions.NotifyPropertyChanged(nameof(x.Name));
                },
                Resources.CategoryEditingData_Main_Name,
                CategoryOptionInheritanceMode.NonInheritable,
                isEditable: CurrentEmployee.HasAnyRole(Role.Admin, Role.Marketer));

            categoryOptions.NameUkrOptionModel = new CategoryOptionViewModel<string>(
                categoryOptions.Parent,
                categoryOptions.Target,
                categoryOptions.Category,
                x => x.NameUkr,
                (x, y) =>
                {
                    x.NameUkr = y;
                    categoryOptions.NotifyPropertyChanged(nameof(x.NameUkr));
                },
                Resources.CategoryEditingData_Main_NameUkr,
                CategoryOptionInheritanceMode.NonInheritable,
                isEditable: CurrentEmployee.HasAnyRole(Role.Admin, Role.Marketer));

            categoryOptions.NameEnOptionModel = new CategoryOptionViewModel<string>(
                categoryOptions.Parent,
                categoryOptions.Target,
                categoryOptions.Category,
                x => x.NameEn,
                (x, y) =>
                {
                    x.NameEn = y;
                    categoryOptions.NotifyPropertyChanged(nameof(x.NameEn));
                },
                Resources.CategoryEditingData_Main_NameEn,
                CategoryOptionInheritanceMode.NonInheritable,
                isEditable: CurrentEmployee.HasAnyRole(Role.Admin, Role.Marketer));

            categoryOptions.ActiveOptionModel = new CategoryOptionViewModel<double>(
                categoryOptions.Parent,
                categoryOptions.Target,
                categoryOptions.Category,
                x => x.Active,
                (x, y) =>
                {
                    x.Active = y;
                    categoryOptions.NotifyPropertyChanged(nameof(x.Active));
                },
                Resources.CategoryEditingData_Main_Active,
                CategoryOptionInheritanceMode.NonInheritable,
                x => categoryOptions.ActiveValues.FirstOrDefault(v => v.Id.Equals(x))?.Name,
                isEditable: CurrentEmployee.HasAnyRole(Role.Admin, Role.Product, Role.Marketer));

            categoryOptions.UseInTradeInOptionModel = new CategoryOptionViewModel<bool>(
                categoryOptions.Parent,
                categoryOptions.Target,
                categoryOptions.Category,
                x => x.UseInTradeIn,
                (x, y) =>
                {
                    x.UseInTradeIn = y;
                    categoryOptions.NotifyPropertyChanged(nameof(x.UseInTradeIn));
                },
                Resources.CategoryEditingData_Main_Use_In_Trade_In,
                CategoryOptionInheritanceMode.NonInheritable,
                isEditable: WebClient.IsOperationAllowed(BusinessOperation.CategorySetUseInTradeIn) && categoryOptions.Category.IsParent);

            categoryOptions.TradeInServiceCostOptionModel = new CategoryOptionViewModel<decimal>(
                categoryOptions.Parent,
                categoryOptions.Target,
                categoryOptions.Category,
                x => x.TradeInServiceCost,
                (x, y) =>
                {
                    x.TradeInServiceCost = y;
                    categoryOptions.NotifyPropertyChanged(nameof(x.TradeInServiceCost));
                },
                Resources.CategoryEditingData_Main_Trade_In_Cost,
                CategoryOptionInheritanceMode.InheritableToCategory,
                isEditable: WebClient.IsOperationAllowed(BusinessOperation.CategorySetTradeInCosts));

            categoryOptions.TradeInServiceCostCurrencyOptionModel = new CategoryOptionViewModel<int>(
                categoryOptions.Parent,
                categoryOptions.Target,
                categoryOptions.Category,
                x => x.TradeInServiceCostCurrencyId,
                (x, y) =>
                {
                    x.TradeInServiceCostCurrencyId = y;
                    categoryOptions.NotifyPropertyChanged(nameof(x.TradeInServiceCostCurrencyId));
                },
                Resources.CategoryEditingData_Main_Trade_In_Cost_Currency,
                CategoryOptionInheritanceMode.InheritableToCategory,
                x =>
                categoryOptions.Currencies.FirstOrDefault(y => y.Id == x)?.Name,
                isEditable: WebClient.IsOperationAllowed(BusinessOperation.CategorySetTradeInCosts));

            categoryOptions.MinCategoryMarginPercentModel = new CategoryOptionViewModel<decimal?>(
                categoryOptions.Parent,
                categoryOptions.Target,
                categoryOptions.Category,
                x => x.MinCategoryMarginPercent,
                (x, y) =>
                {
                    x.MinCategoryMarginPercent = y;
                    categoryOptions.NotifyPropertyChanged(nameof(x.MinCategoryMarginPercent));
                },
                "Минимальная маржа по категории, %",
                CategoryOptionInheritanceMode.InheritableToCategory,
                isEditable: WebClient.IsOperationAllowed(BusinessOperation.CreditOfferMinCategoryMarginPercentUpdate) && categoryOptions.Category.IsParent);

            categoryOptions.YandexMarketHidOptionModel = new CategoryOptionViewModel<int>(
                categoryOptions.Parent,
                categoryOptions.Target,
                categoryOptions.Category,
                x => x.YandexMarketHid,
                (x, y) =>
                {
                    x.YandexMarketHid = y;
                    categoryOptions.NotifyPropertyChanged(nameof(x.YandexMarketHid));
                },
                Resources.CategoryEditingData_Main_YandexMarketHid,
                CategoryOptionInheritanceMode.InheritableToCategory,
                isEditable: CurrentEmployee.HasAnyRole(Role.Admin, Role.Marketer));

            categoryOptions.ManufactorOptionModel = new CategoryOptionViewModel<string>(
                categoryOptions.Parent,
                categoryOptions.Target,
                categoryOptions.Category,
                x => x.Manufactor,
                (x, y) =>
                {
                    x.Manufactor = y;
                    categoryOptions.NotifyPropertyChanged(nameof(x.Manufactor));
                },
                Resources.CategoryEditingData_Main_Manufactor,
                CategoryOptionInheritanceMode.InheritableToCategoryAndProduct,
                isEditable: CurrentEmployee.HasAnyRole(Role.Admin, Role.Marketer));

            categoryOptions.RobotScriptOptionModel = new CategoryOptionViewModel<string>(
                categoryOptions.Parent,
                categoryOptions.Target,
                categoryOptions.Category,
                x => x.RobotScript,
                (x, y) =>
                {
                    x.RobotScript = y;
                    categoryOptions.NotifyPropertyChanged(nameof(x.RobotScript));
                },
                Resources.CategoryEditingData_Robot_RobotScript,
                CategoryOptionInheritanceMode.InheritableToCategory,
                isEditable: CurrentEmployee.HasAnyRole(Role.Admin, Role.Product, Role.Marketer));

            categoryOptions.RobotScriptParametersOptionModel = new CategoryOptionViewModel<string>(
                categoryOptions.Parent,
                categoryOptions.Target,
                categoryOptions.Category,
                x => x.RobotScriptParameters,
                (x, y) =>
                {
                    x.RobotScriptParameters = y;
                    categoryOptions.NotifyPropertyChanged(nameof(x.RobotScriptParameters));
                },
                Resources.CategoryEditingData_Robot_RobotScriptParameters,
                CategoryOptionInheritanceMode.InheritableToCategory,
                isEditable: CurrentEmployee.HasAnyRole(Role.Admin, Role.Product, Role.Marketer));

            categoryOptions.RobotModeManualIdOptionModel = new CategoryOptionViewModel<int>(
                categoryOptions.Parent,
                categoryOptions.Target,
                categoryOptions.Category,
                x => x.RobotModeManualId,
                (x, y) =>
                {
                    x.RobotModeManualId = y;
                    categoryOptions.NotifyPropertyChanged(nameof(x.RobotModeManualId));
                },
                Resources.CategoryEditingData_Robot_RobotModeManual,
                CategoryOptionInheritanceMode.InheritableToCategoryAndProduct,
                x => categoryOptions.PriceRobotManualModes.FirstOrDefault(y => y.Id == x)?.Name,
                CurrentEmployee.HasAnyRole(Role.Admin, Role.Product, Role.Marketer));

            categoryOptions.RobotModeAutoIdOptionModel = new CategoryOptionViewModel<int>(
                categoryOptions.Parent,
                categoryOptions.Target,
                categoryOptions.Category,
                x => x.RobotModeAutoId,
                (x, y) =>
                {
                    x.RobotModeAutoId = y;
                    categoryOptions.NotifyPropertyChanged(nameof(x.RobotModeAutoId));
                },
                Resources.CategoryEditingData_Robot_RobotModeAuto,
                CategoryOptionInheritanceMode.InheritableToCategoryAndProduct,
                x => categoryOptions.PriceRobotAutoModes.FirstOrDefault(y => y.Id == x)?.Name,
                CurrentEmployee.HasAnyRole(Role.Admin, Role.Product, Role.Marketer));

            categoryOptions.HotlineOptionModel = new CategoryOptionViewModel<bool>(
                categoryOptions.Parent,
                categoryOptions.Target,
                categoryOptions.Category,
                x => x.Hotline,
                (x, y) =>
                {
                    x.Hotline = y;
                    categoryOptions.NotifyPropertyChanged(nameof(x.Hotline));
                },
                Resources.CategoryEditingData_TradingPlaces_Hotline,
                CategoryOptionInheritanceMode.InheritableToCategoryAndProduct,
                isEditable: CurrentEmployee.HasAnyRole(Role.Admin, Role.Product, Role.Marketer));

            categoryOptions.EmployeeIdOptionModel = new CategoryOptionViewModel<int>(
                categoryOptions.Parent,
                categoryOptions.Target,
                categoryOptions.Category,
                x => x.EmployeeId,
                (x, y) =>
                {
                    x.EmployeeId = y;
                    categoryOptions.NotifyPropertyChanged(nameof(x.EmployeeId));
                },
                Resources.CategoryEditingData_Properties_EmployeeId,
                CategoryOptionInheritanceMode.InheritableToCategoryAndProduct,
                x => categoryOptions.Employees.FirstOrDefault(y => y.Id == x)?.Name,
                isEditable: CurrentEmployee.HasAnyRole(Role.Admin, Role.Marketer));

            categoryOptions.EmployeeSupIdOptionModel = new CategoryOptionViewModel<int>(
                categoryOptions.Parent,
                categoryOptions.Target,
                categoryOptions.Category,
                x => x.EmployeeSupId,
                (x, y) =>
                {
                    x.EmployeeSupId = y;
                    categoryOptions.NotifyPropertyChanged(nameof(x.EmployeeSupId));
                },
                Resources.CategoryEditingData_Properties_EmployeeSupId,
                CategoryOptionInheritanceMode.InheritableToCategoryAndProduct,
                x => categoryOptions.Employees.FirstOrDefault(y => y.Id == x)?.Name,
                isEditable: CurrentEmployee.HasAnyRole(Role.Admin, Role.Marketer, Role.Product));

            categoryOptions.WarrantyTypeIdOptionModel = new CategoryOptionViewModel<int?>(
              categoryOptions.Parent,
              categoryOptions.Target,
              categoryOptions.Category,
              x => x.WarrantyTypeId,
              (x, y) =>
              {
                  x.WarrantyTypeId = y;
                  categoryOptions.NotifyPropertyChanged(nameof(x.WarrantyTypeId));
              },
              "Тип гарантии",
              CategoryOptionInheritanceMode.InheritableToCategoryAndProduct,
              x => categoryOptions.WarrantyTypes.FirstOrDefault(y => y.Id == x)?.Name,
              isEditable: CurrentEmployee.HasAnyRole(Role.Admin, Role.Marketer, Role.Product));

            categoryOptions.KindOptionModel = new CategoryOptionViewModel<string>(
                categoryOptions.Parent,
                categoryOptions.Target,
                categoryOptions.Category,
                x => x.Kind,
                (x, y) =>
                {
                    x.Kind = y;
                    categoryOptions.NotifyPropertyChanged(nameof(x.Kind));
                },
                Resources.CategoryEditingData_Properties_Kind,
                CategoryOptionInheritanceMode.InheritableToCategoryAndProduct,
                x => categoryOptions.Kinds.FirstOrDefault(y => y.Name == x)?.Title,
                isEditable: CurrentEmployee.HasAnyRole(Role.Admin, Role.Marketer));

            categoryOptions.CurrencyOptionModel = new CategoryOptionViewModel<string>(
                categoryOptions.Parent,
                categoryOptions.Target,
                categoryOptions.Category,
                x => x.Currency,
                (x, y) =>
                {
                    x.Currency = y;
                    categoryOptions.NotifyPropertyChanged(nameof(x.Currency));
                },
                Resources.CategoryEditingData_Properties_Currency,
                CategoryOptionInheritanceMode.InheritableToCategoryAndProduct,
                x => categoryOptions.Currencies.FirstOrDefault(y => y.Name == x)?.Title,
                isEditable: CurrentEmployee.HasAnyRole(Role.Admin, Role.Product, Role.Marketer));

            categoryOptions.UsdCurrencyOptionModel = new CategoryOptionViewModel<int>(
                categoryOptions.Parent,
                categoryOptions.Target,
                categoryOptions.Category,
                x => x.UsdCurrency,
                (x, y) =>
                {
                    x.UsdCurrency = y;
                    categoryOptions.NotifyPropertyChanged(nameof(x.UsdCurrency));
                },
                Resources.CategoryEditingData_Properties_UsdCurrency,
                CategoryOptionInheritanceMode.InheritableToCategoryAndProduct,
                x => categoryOptions.UsdCurrencies.FirstOrDefault(y => y.Id == x)?.Title,
                isEditable: CurrentEmployee.HasAnyRole(Role.Admin, Role.Marketer));

            categoryOptions.ShortNamesFor1CModel = new CategoryOptionViewModel<bool>(
                categoryOptions.Parent,
                categoryOptions.Target,
                categoryOptions.Category,
                x => x.ShortNamesFor1C,
                (x, y) =>
                {
                    x.ShortNamesFor1C = y;
                    categoryOptions.NotifyPropertyChanged(nameof(x.ShortNamesFor1C));
                },
                Resources.CategoryEditingData_Properties_ShortName1c,
                CategoryOptionInheritanceMode.InheritableToCategory,
                isEditable: WebClient.IsOperationAllowed(BusinessOperation.CategorySetShortNamesFor1C));

            categoryOptions.PlannedMarkupModel = new CategoryOptionViewModel<decimal>(
                categoryOptions.Parent,
                categoryOptions.Target,
                categoryOptions.Category,
                x => x.PlannedMarkup,
                (x, y) =>
                {
                    x.PlannedMarkup = y;
                    categoryOptions.NotifyPropertyChanged(nameof(x.PlannedMarkup));
                },
                "Плановая рентабельность, %",
                CategoryOptionInheritanceMode.InheritableToCategoryAndProduct,
                isEditable: WebClient.IsOperationAllowed(BusinessOperation.CategorySetPlannedMarkup));

            categoryOptions.UseNewRobotModel = new CategoryOptionViewModel<bool>(
                categoryOptions.Parent,
                categoryOptions.Target,
                categoryOptions.Category,
                x => x.UseNewRobot,
                (x, y) =>
                {
                    x.UseNewRobot = y;
                    categoryOptions.NotifyPropertyChanged(nameof(x.UseNewRobot));
                },
                "Использовать новый робот",
                CategoryOptionInheritanceMode.InheritableToCategory,
                isEditable: WebClient.IsOperationAllowed(BusinessOperation.RobotCategoryPropertyValuesAllowEdit));

            categoryOptions.UsePlannedMarkupInAutoShowcaseModel = new CategoryOptionViewModel<bool>(
                categoryOptions.Parent,
                categoryOptions.Target,
                categoryOptions.Category,
                x => x.UsePlannedMarkupInAutoShowcase,
                (x, y) =>
                {
                    x.UsePlannedMarkupInAutoShowcase = y;
                    categoryOptions.NotifyPropertyChanged(nameof(x.UsePlannedMarkupInAutoShowcase));
                },
                "Использовать рентабельнсть в автовитрине",
                CategoryOptionInheritanceMode.InheritableToCategoryAndProduct,
                isEditable: WebClient.IsOperationAllowed(BusinessOperation.CategorySetPlannedMarkup));

            categoryOptions.ShowcaseSkuLimitModel = new CategoryOptionViewModel<int>(
                categoryOptions.Parent,
                categoryOptions.Target,
                categoryOptions.Category,
                x => x.ShowcaseSkuLimit,
                (x, y) =>
                {
                    x.ShowcaseSkuLimit = y;
                    categoryOptions.NotifyPropertyChanged(nameof(x.ShowcaseSkuLimit));
                },
                "Лимит одного sku на витрине",
                CategoryOptionInheritanceMode.InheritableToCategory,
                isEditable: WebClient.IsOperationAllowed(BusinessOperation.CategorySetShowcaseSkuLimit));

            categoryOptions.WarrantyRetailIdOptionModel = new CategoryOptionViewModel<int>(
                categoryOptions.Parent,
                categoryOptions.Target,
                categoryOptions.Category,
                x => x.WarrantyRetailId,
                (x, y) =>
                {
                    x.WarrantyRetailId = y;
                    categoryOptions.NotifyPropertyChanged(nameof(x.WarrantyRetailId));
                },
                Resources.CategoryEditingData_Properties_WarrantyRetail,
                CategoryOptionInheritanceMode.InheritableToCategoryAndProduct,
                x =>
                categoryOptions.Warranties.FirstOrDefault(y => y.Id.Equals(x))?.Name,
                isEditable: CurrentEmployee.HasAnyRole(Role.Admin, Role.Product, Role.Marketer));

            categoryOptions.WarrantyWholesaleIdOptionModel = new CategoryOptionViewModel<int>(
                categoryOptions.Parent,
                categoryOptions.Target,
                categoryOptions.Category,
                x => x.WarrantyWholesaleId,
                (x, y) =>
                {
                    x.WarrantyWholesaleId = y;
                    categoryOptions.NotifyPropertyChanged(nameof(x.WarrantyWholesaleId));
                },
                Resources.CategoryEditingData_Properties_WarrantyWholesale,
                CategoryOptionInheritanceMode.InheritableToCategoryAndProduct,
                x =>
                categoryOptions.Warranties.FirstOrDefault(y => y.Id.Equals(x))?.Name,
                isEditable: CurrentEmployee.HasAnyRole(Role.Admin, Role.Product, Role.Marketer));

            categoryOptions.PrintWarrantyCardOptionModel = new CategoryOptionViewModel<bool>(
                categoryOptions.Parent,
                categoryOptions.Target,
                categoryOptions.Category,
                x => x.PrintWarrantyCard,
                (x, y) =>
                {
                    x.PrintWarrantyCard = y;
                    categoryOptions.NotifyPropertyChanged(nameof(x.PrintWarrantyCard));
                },
                Resources.CategoryEditingData_Properties_PrintWarrantyCard,
                CategoryOptionInheritanceMode.InheritableToCategoryAndProduct,
                isEditable: CurrentEmployee.HasAnyRole(Role.Admin, Role.Product, Role.Marketer));

            categoryOptions.KeepSerialOptionModel = new CategoryOptionViewModel<bool>(
                categoryOptions.Parent,
                categoryOptions.Target,
                categoryOptions.Category,
                x => x.KeepSerial,
                (x, y) =>
                {
                    x.KeepSerial = y;
                    categoryOptions.NotifyPropertyChanged(nameof(x.KeepSerial));
                },
                Resources.CategoryEditingData_Properties_KeepSerial,
                CategoryOptionInheritanceMode.InheritableToCategoryAndProduct,
                isEditable: CurrentEmployee.HasAnyRole(Role.Admin, Role.Marketer));

            categoryOptions.KeepPnOptionModel = new CategoryOptionViewModel<bool>(
                categoryOptions.Parent,
                categoryOptions.Target,
                categoryOptions.Category,
                x => x.KeepPn,
                (x, y) =>
                {
                    x.KeepPn = y;
                    categoryOptions.NotifyPropertyChanged(nameof(x.KeepPn));
                },
                Resources.CategoryEditingData_Properties_KeepPn,
                CategoryOptionInheritanceMode.InheritableToCategory,
                isEditable: CurrentEmployee.HasAnyRole(Role.Admin, Role.Marketer));

            categoryOptions.WeightEstimatedOptionModel = new CategoryOptionViewModel<double>(
                categoryOptions.Parent,
                categoryOptions.Target,
                categoryOptions.Category,
                x => x.WeightEstimated,
                (x, y) =>
                {
                    x.WeightEstimated = y;
                    categoryOptions.NotifyPropertyChanged(nameof(x.WeightEstimated));
                },
                Resources.CategoryEditingData_Properties_Weight,
                CategoryOptionInheritanceMode.InheritableToCategoryAndProduct,
                isEditable: CurrentEmployee.HasAnyRole(Role.Admin, Role.Product, Role.Marketer));

            categoryOptions.FreeDeliveryOptionModel = new CategoryOptionViewModel<bool>(
                categoryOptions.Parent,
                categoryOptions.Target,
                categoryOptions.Category,
                x => x.FreeDelivery,
                (x, y) =>
                {
                    x.FreeDelivery = y;
                    categoryOptions.NotifyPropertyChanged(nameof(x.FreeDelivery));
                },
                Resources.CategoryEditingData_Properties_FreeDelivery,
                CategoryOptionInheritanceMode.InheritableToCategoryAndProduct,
                isEditable: CurrentEmployee.HasAnyRole(Role.Admin, Role.Marketer));

            categoryOptions.SelfBarcodeOptionModel = new CategoryOptionViewModel<bool>(
                categoryOptions.Parent,
                categoryOptions.Target,
                categoryOptions.Category,
                x => x.SelfBarcode,
                (x, y) =>
                {
                    x.SelfBarcode = y;
                    categoryOptions.NotifyPropertyChanged(nameof(x.SelfBarcode));
                },
                Resources.CategoryEditingData_Properties_SelfBarcode,
                CategoryOptionInheritanceMode.InheritableToCategoryAndProduct,
                isEditable: CurrentEmployee.HasAnyRole(Role.Admin, Role.Marketer));

            categoryOptions.KeepDimensionsOptionModel = new CategoryOptionViewModel<bool>(
                categoryOptions.Parent,
                categoryOptions.Target,
                categoryOptions.Category,
                x => x.KeepDimensions,
                (x, y) =>
                {
                    x.KeepDimensions = y;
                    categoryOptions.NotifyPropertyChanged(nameof(x.KeepDimensions));
                },
                Resources.CategoryEditingData_Properties_KeepDimensions,
                CategoryOptionInheritanceMode.InheritableToCategory,
                isEditable: CurrentEmployee.HasAnyRole(Role.Admin, Role.Marketer));

            categoryOptions.StickerFragileOptionModel = new CategoryOptionViewModel<bool>(
                categoryOptions.Parent,
                categoryOptions.Target,
                categoryOptions.Category,
                x => x.StickerFragile,
                (x, y) =>
                {
                    x.StickerFragile = y;
                    categoryOptions.NotifyPropertyChanged(nameof(x.StickerFragile));
                },
                Resources.CategoryEditingData_Properties_StickerFragile,
                CategoryOptionInheritanceMode.InheritableToCategory,
                isEditable: WebClient.IsOperationAllowed(BusinessOperation.CategoryStickerEdit) && categoryOptions.Category.IsParent);

            categoryOptions.StickerThisWayUpOptionModel = new CategoryOptionViewModel<bool>(
                categoryOptions.Parent,
                categoryOptions.Target,
                categoryOptions.Category,
                x => x.StickerThisWayUp,
                (x, y) =>
                {
                    x.StickerThisWayUp = y;
                    categoryOptions.NotifyPropertyChanged(nameof(x.StickerThisWayUp));
                },
                Resources.CategoryEditingData_Properties_StickerThisWayUp,
                CategoryOptionInheritanceMode.InheritableToCategory,
                isEditable: WebClient.IsOperationAllowed(BusinessOperation.CategoryStickerEdit) && categoryOptions.Category.IsParent);

            categoryOptions.DaysToFillOptionModel = new CategoryOptionViewModel<int>(
                categoryOptions.Parent,
                categoryOptions.Target,
                categoryOptions.Category,
                x => x.DaysToFill,
                (x, y) =>
                {
                    x.DaysToFill = y;
                    categoryOptions.NotifyPropertyChanged(nameof(x.DaysToFill));
                },
                Resources.CategoryEditingData_Content_DaysToFill,
                CategoryOptionInheritanceMode.InheritableToCategory,
                isEditable: CurrentEmployee.HasAnyRole(Role.Admin, Role.Marketer));

            categoryOptions.NeedContentOptionModel = new CategoryOptionViewModel<bool>(
                categoryOptions.Parent,
                categoryOptions.Target,
                categoryOptions.Category,
                x => x.NeedContent,
                (x, y) =>
                {
                    x.NeedContent = y;
                    categoryOptions.NotifyPropertyChanged(nameof(x.NeedContent));
                },
                Resources.CategoryEditingData_Content_NeedContent,
                CategoryOptionInheritanceMode.InheritableToCategory,
                isEditable: CurrentEmployee.HasAnyRole(Role.Admin, Role.Marketer));

            categoryOptions.NeedVideoOptionModel = new CategoryOptionViewModel<bool>(
                categoryOptions.Parent,
                categoryOptions.Target,
                categoryOptions.Category,
                x => x.NeedVideo,
                (x, y) =>
                {
                    x.NeedVideo = y;
                    categoryOptions.NotifyPropertyChanged(nameof(x.NeedVideo));
                },
                Resources.CategoryEditingData_Content_NeedVideo,
                CategoryOptionInheritanceMode.InheritableToCategory,
                isEditable: CurrentEmployee.HasAnyRole(Role.Admin, Role.Marketer));

            categoryOptions.NeedComplectOptionModel = new CategoryOptionViewModel<bool>(
                categoryOptions.Parent,
                categoryOptions.Target,
                categoryOptions.Category,
                x => x.NeedComplect,
                (x, y) =>
                {
                    x.NeedComplect = y;
                    categoryOptions.NotifyPropertyChanged(nameof(x.NeedComplect));
                },
                Resources.CategoryEditingData_Content__NeedComplect,
                CategoryOptionInheritanceMode.InheritableToCategory,
                isEditable: CurrentEmployee.HasAnyRole(Role.Admin, Role.Marketer));

            categoryOptions.NeedDescriptionOptionModel = new CategoryOptionViewModel<int>(
                categoryOptions.Parent,
                categoryOptions.Target,
                categoryOptions.Category,
                x => x.NeedDescription,
                (x, y) =>
                {
                    x.NeedDescription = y;
                    categoryOptions.NotifyPropertyChanged(nameof(x.NeedDescription));
                },
                Resources.CategoryEditingData_Content_NeedDescription,
                CategoryOptionInheritanceMode.InheritableToCategory,
                isEditable: CurrentEmployee.HasAnyRole(Role.Admin, Role.Marketer));

            categoryOptions.NeedPhotoOptionModel = new CategoryOptionViewModel<int>(
                categoryOptions.Parent,
                categoryOptions.Target,
                categoryOptions.Category,
                x => x.NeedPhoto,
                (x, y) =>
                {
                    x.NeedPhoto = y;
                    categoryOptions.NotifyPropertyChanged(nameof(x.NeedPhoto));
                },
                Resources.CategoryEditingData_Content_NeedPhoto,
                CategoryOptionInheritanceMode.InheritableToCategory,
                isEditable: CurrentEmployee.HasAnyRole(Role.Admin, Role.Marketer));

            categoryOptions.PrefixRusOptionModel = new CategoryOptionViewModel<string>(
                categoryOptions.Parent,
                categoryOptions.Target,
                categoryOptions.Category,
                x => x.PrefixRus,
                (x, y) =>
                {
                    x.PrefixRus = y;
                    categoryOptions.NotifyPropertyChanged(nameof(x.PrefixRus));
                },
                Resources.CategoryEditingData_Prefix_PrefixRus,
                CategoryOptionInheritanceMode.InheritableToCategoryAndProduct,
                isEditable: CurrentEmployee.HasAnyRole(Role.Admin, Role.Marketer));

            categoryOptions.PrefixUkrOptionModel = new CategoryOptionViewModel<string>(
                categoryOptions.Parent,
                categoryOptions.Target,
                categoryOptions.Category,
                x => x.PrefixUkr,
                (x, y) =>
                {
                    x.PrefixUkr = y;
                    categoryOptions.NotifyPropertyChanged(nameof(x.PrefixUkr));
                },
                Resources.CategoryEditingData_Prefix_PrefixUkr,
                CategoryOptionInheritanceMode.InheritableToCategoryAndProduct,
                isEditable: CurrentEmployee.HasAnyRole(Role.Admin, Role.Marketer));

            categoryOptions.PrefixEnOptionModel = new CategoryOptionViewModel<string>(
                categoryOptions.Parent,
                categoryOptions.Target,
                categoryOptions.Category,
                x => x.PrefixEn,
                (x, y) =>
                {
                    x.PrefixEn = y;
                    categoryOptions.NotifyPropertyChanged(nameof(x.PrefixEn));
                },
                Resources.CategoryEditingData_Prefix_PrefixEn,
                CategoryOptionInheritanceMode.InheritableToCategoryAndProduct,
                isEditable: CurrentEmployee.HasAnyRole(Role.Admin, Role.Marketer));

            categoryOptions.TagFormatIdOptionModel = new CategoryOptionViewModel<int>(
                categoryOptions.Parent,
                categoryOptions.Target,
                categoryOptions.Category,
                x => x.TagFormatId,
                (x, y) =>
                {
                    x.TagFormatId = y;
                    categoryOptions.NotifyPropertyChanged(nameof(x.TagFormatId));
                },
                Resources.CategoryEditingData_TagFormat,
                CategoryOptionInheritanceMode.InheritableToCategory,
                x => categoryOptions.TagFormats.FirstOrDefault(y => y.Id == x)?.Title,
                isEditable: CurrentEmployee.HasAnyRole(Role.Admin, Role.Marketer));

            categoryOptions.TaxRateIdOptionModel = new CategoryOptionViewModel<int>(
                categoryOptions.Parent,
                categoryOptions.Target,
                categoryOptions.Category,
                x => x.TaxRateId,
                (x, y) =>
                {
                    x.TaxRateId = y;
                    categoryOptions.NotifyPropertyChanged(nameof(x.TaxRateId));
                },
                Resources.CategoryEditingData_TaxRate,
                CategoryOptionInheritanceMode.InheritableToCategoryAndProduct,
                x => categoryOptions.TaxRates.FirstOrDefault(y => y.Id == x)?.Name,
                isEditable: CurrentEmployee.HasAnyRole(Role.Admin, Role.Product, Role.Accountant));

            categoryOptions.TypeIdOptionModel = new CategoryOptionViewModel<int?>(
                categoryOptions.Parent,
                categoryOptions.Target,
                categoryOptions.Category,
                x => x.TypeId,
                (x, y) =>
                {
                    x.TypeId = y;
                    categoryOptions.NotifyPropertyChanged(nameof(x.TypeId));
                },
                Resources.CategoryEditingData_Type,
                CategoryOptionInheritanceMode.InheritableToCategory,
                x => categoryOptions.Types.FirstOrDefault(y => y.Id == x)?.Name,
                isEditable: WebClient.IsOperationAllowed(BusinessOperation.CategorySetType));

            categoryOptions.AdmitadTariffCodeOptionModel = new CategoryOptionViewModel<int?>(
                categoryOptions.Parent,
                categoryOptions.Target,
                categoryOptions.Category,
                x => x.AdmitadTariffCodeId,
                (x, y) =>
                {
                    x.AdmitadTariffCodeId = y;
                    categoryOptions.NotifyPropertyChanged(nameof(x.AdmitadTariffCodeId));
                },
                Resources.CategoryEditingData_AdmitadTariffCode,
                CategoryOptionInheritanceMode.InheritableToCategoryAndProduct,
                x => categoryOptions.AdmitadTariffCodes.FirstOrDefault(y => y.Id == x)?.Name,
                isEditable: WebClient.IsOperationAllowed(BusinessOperation.CategorySetAdmitadTariffCode));

            categoryOptions.SalesDoublerTariffCodeOptionModel = new CategoryOptionViewModel<int?>(
                categoryOptions.Parent,
                categoryOptions.Target,
                categoryOptions.Category,
                x => x.SalesDoublerTariffCodeId,
                (x, y) =>
                {
                    x.SalesDoublerTariffCodeId = y;
                    categoryOptions.NotifyPropertyChanged(nameof(x.SalesDoublerTariffCodeId));
                },
                Resources.CategoryEditingData_SalesDoublerTariffCode,
                CategoryOptionInheritanceMode.InheritableToCategoryAndProduct,
                x => categoryOptions.SalesDoublerTariffCodes.FirstOrDefault(y => y.Id == x)?.Name,
                isEditable: WebClient.IsOperationAllowed(BusinessOperation.CategorySetSalesDoublerTariffCode));

            categoryOptions.ReferralDiscountCodeOptionModel = new CategoryOptionViewModel<int?>(
                categoryOptions.Parent,
                categoryOptions.Target,
                categoryOptions.Category,
                x => x.ReferralDiscountCodeId,
                (x, y) =>
                {
                    x.ReferralDiscountCodeId = y;
                    categoryOptions.NotifyPropertyChanged(nameof(x.ReferralDiscountCodeId));
                },
                "Тариф реферальной программы",
                CategoryOptionInheritanceMode.InheritableToCategoryAndProduct,
                x => categoryOptions.ReferralDiscountCodes.FirstOrDefault(y => y.Id == x)?.Name,
                isEditable: WebClient.IsOperationAllowed(BusinessOperation.CategorySetReferralDiscountCode));

            categoryOptions.SegmentLimitOptionModel = new CategoryOptionViewModel<int?>(
                categoryOptions.Parent,
                categoryOptions.Target,
                categoryOptions.Category,
                x => x.SegmentLimit,
                (x, y) =>
                {
                    x.SegmentLimit = y;
                    categoryOptions.NotifyPropertyChanged(nameof(x.SegmentLimit));
                },
                "Лимит сегментов",
                CategoryOptionInheritanceMode.InheritableToCategory,
                isEditable: WebClient.IsOperationAllowed(BusinessOperation.ManageSegmentLimits) && categoryOptions.Category.IsParent);

            categoryOptions.SegmentFeaturesLimitOptionModel = new CategoryOptionViewModel<int?>(
                categoryOptions.Parent,
                categoryOptions.Target,
                categoryOptions.Category,
                x => x.SegmentFeaturesLimit,
                (x, y) =>
                {
                    x.SegmentFeaturesLimit = y;
                    categoryOptions.NotifyPropertyChanged(nameof(x.SegmentFeaturesLimit));
                },
                "Лимит сегм. хар-ик",
                CategoryOptionInheritanceMode.InheritableToCategory,
                isEditable: WebClient.IsOperationAllowed(BusinessOperation.ManageSegmentLimits) && categoryOptions.Category.IsParent);

            categoryOptions.NameTransOptionModel = new CategoryOptionViewModel<string>(
               categoryOptions.Parent,
               categoryOptions.Target,
               categoryOptions.Category,
               x => x.NameTrans,
               (x, y) =>
               {
                   x.NameTrans = y;
                   categoryOptions.NotifyPropertyChanged(nameof(x.NameTrans));
               },
               "Название транслит (рус.)",
               CategoryOptionInheritanceMode.NonInheritable,
               isEditable: CurrentEmployee.HasAnyRole(Role.Admin, Role.Marketer));

            categoryOptions.NameTransUkrOptionModel = new CategoryOptionViewModel<string>(
                categoryOptions.Parent,
                categoryOptions.Target,
                categoryOptions.Category,
                x => x.NameTransUkr,
                (x, y) =>
                {
                    x.NameTransUkr = y;
                    categoryOptions.NotifyPropertyChanged(nameof(x.NameTransUkr));
                },
                "Название транслит (укр.)",
                CategoryOptionInheritanceMode.NonInheritable,
                isEditable: CurrentEmployee.HasAnyRole(Role.Admin, Role.Marketer));

            categoryOptions.NameTransEnOptionModel = new CategoryOptionViewModel<string>(
                categoryOptions.Parent,
                categoryOptions.Target,
                categoryOptions.Category,
                x => x.NameTransEn,
                (x, y) =>
                {
                    x.NameTransEn = y;
                    categoryOptions.NotifyPropertyChanged(nameof(x.NameTransEn));
                },
                "Название транслит (англ.)",
                CategoryOptionInheritanceMode.NonInheritable,
                isEditable: CurrentEmployee.HasAnyRole(Role.Admin, Role.Marketer));

            categoryOptions.NameBreadcrumbsOptionModel = new CategoryOptionViewModel<string>(
                categoryOptions.Parent,
                categoryOptions.Target,
                categoryOptions.Category,
                x => x.NameBreadcrumbs,
                (x, y) =>
                {
                    x.NameBreadcrumbs = y;
                    categoryOptions.NotifyPropertyChanged(nameof(x.NameBreadcrumbs));
                },
                "Назв. в хлеб. крошках (рус.)",
                CategoryOptionInheritanceMode.NonInheritable,
                isEditable: CurrentEmployee.HasAnyRole(Role.Admin, Role.Marketer));

            categoryOptions.NameBreadcrumbsUkrOptionModel = new CategoryOptionViewModel<string>(
                categoryOptions.Parent,
                categoryOptions.Target,
                categoryOptions.Category,
                x => x.NameBreadcrumbsUkr,
                (x, y) =>
                {
                    x.NameBreadcrumbsUkr = y;
                    categoryOptions.NotifyPropertyChanged(nameof(x.NameBreadcrumbsUkr));
                },
                "Назв. в хлеб. крошках (укр.)",
                CategoryOptionInheritanceMode.NonInheritable,
                isEditable: CurrentEmployee.HasAnyRole(Role.Admin, Role.Marketer));

            categoryOptions.NameBreadcrumbsEnOptionModel = new CategoryOptionViewModel<string>(
                categoryOptions.Parent,
                categoryOptions.Target,
                categoryOptions.Category,
                x => x.NameBreadcrumbsEn,
                (x, y) =>
                {
                    x.NameBreadcrumbsEn = y;
                    categoryOptions.NotifyPropertyChanged(nameof(x.NameBreadcrumbsEn));
                },
                "Назв. в хлеб. крошках (англ.)",
                CategoryOptionInheritanceMode.NonInheritable,
                isEditable: CurrentEmployee.HasAnyRole(Role.Admin, Role.Marketer));

            categoryOptions.DescriptionOptionModel = new CategoryOptionViewModel<string>(
                categoryOptions.Parent,
                categoryOptions.Target,
                categoryOptions.Category,
                x => x.Description,
                (x, y) =>
                {
                    x.Description = y;
                    categoryOptions.NotifyPropertyChanged(nameof(x.Description));
                },
                "Описание (рус.)",
                CategoryOptionInheritanceMode.NonInheritable,
                isEditable: CurrentEmployee.HasAnyRole(Role.Admin, Role.Marketer));

            categoryOptions.DescriptionUkrOptionModel = new CategoryOptionViewModel<string>(
                categoryOptions.Parent,
                categoryOptions.Target,
                categoryOptions.Category,
                x => x.DescriptionUkr,
                (x, y) =>
                {
                    x.DescriptionUkr = y;
                    categoryOptions.NotifyPropertyChanged(nameof(x.DescriptionUkr));
                },
                "Описание (укр.)",
                CategoryOptionInheritanceMode.NonInheritable,
                isEditable: CurrentEmployee.HasAnyRole(Role.Admin, Role.Marketer));

            categoryOptions.DescriptionEnOptionModel = new CategoryOptionViewModel<string>(
                categoryOptions.Parent,
                categoryOptions.Target,
                categoryOptions.Category,
                x => x.DescriptionEn,
                (x, y) =>
                {
                    x.DescriptionEn = y;
                    categoryOptions.NotifyPropertyChanged(nameof(x.DescriptionEn));
                },
                "Описание (англ.)",
                CategoryOptionInheritanceMode.NonInheritable,
                isEditable: CurrentEmployee.HasAnyRole(Role.Admin, Role.Marketer));

            categoryOptions.PromoInfoOptionModel = new CategoryOptionViewModel<string>(
                categoryOptions.Parent,
                categoryOptions.Target,
                categoryOptions.Category,
                x => x.PromoInfo,
                (x, y) =>
                {
                    x.PromoInfo = y;
                    categoryOptions.NotifyPropertyChanged(nameof(x.PromoInfo));
                },
                "Промо текст (рус.)",
                CategoryOptionInheritanceMode.NonInheritable,
                isEditable: CurrentEmployee.HasAnyRole(Role.Admin, Role.Marketer));

            categoryOptions.PromoInfoUkrOptionModel = new CategoryOptionViewModel<string>(
                categoryOptions.Parent,
                categoryOptions.Target,
                categoryOptions.Category,
                x => x.PromoInfoUkr,
                (x, y) =>
                {
                    x.PromoInfoUkr = y;
                    categoryOptions.NotifyPropertyChanged(nameof(x.PromoInfoUkr));
                },
                "Промо текст (укр.)",
                CategoryOptionInheritanceMode.NonInheritable,
                isEditable: CurrentEmployee.HasAnyRole(Role.Admin, Role.Marketer));

            categoryOptions.PromoInfoEnOptionModel = new CategoryOptionViewModel<string>(
                categoryOptions.Parent,
                categoryOptions.Target,
                categoryOptions.Category,
                x => x.PromoInfoEn,
                (x, y) =>
                {
                    x.PromoInfoEn = y;
                    categoryOptions.NotifyPropertyChanged(nameof(x.PromoInfoEn));
                },
                "Промо текст (англ.)",
                CategoryOptionInheritanceMode.NonInheritable,
                isEditable: CurrentEmployee.HasAnyRole(Role.Admin, Role.Marketer));

            categoryOptions.CategoryPriorityOptionModel = new CategoryOptionViewModel<int?>(
                categoryOptions.Parent,
                categoryOptions.Target,
                categoryOptions.Category,
                x => x.CategoryPriority,
                (x, y) =>
                {
                    x.CategoryPriority = y;
                    categoryOptions.NotifyPropertyChanged(nameof(x.CategoryPriority));
                },
                "Приоритет категории",
                CategoryOptionInheritanceMode.InheritableToCategory,
                isEditable: WebClient.IsOperationAllowed(BusinessOperation.CategoryKpiEdit),
                isVisible: WebClient.IsOperationAllowed(BusinessOperation.CategoryKpiView) || WebClient.IsOperationAllowed(BusinessOperation.CategoryKpiEdit));

            categoryOptions.PlannedTurnoverOptionModel = new CategoryOptionViewModel<decimal?>(
                categoryOptions.Parent,
                categoryOptions.Target,
                categoryOptions.Category,
                x => x.PlannedTurnover,
                (x, y) =>
                {
                    x.PlannedTurnover = y;
                    categoryOptions.NotifyPropertyChanged(nameof(x.PlannedTurnover));
                },
                "Плановый оборот",
                CategoryOptionInheritanceMode.InheritableToCategory,
                isEditable: WebClient.IsOperationAllowed(BusinessOperation.CategoryKpiEdit),
                isVisible: WebClient.IsOperationAllowed(BusinessOperation.CategoryKpiView) || WebClient.IsOperationAllowed(BusinessOperation.CategoryKpiEdit));

            categoryOptions.PlannedGrossProfitOptionModel = new CategoryOptionViewModel<decimal?>(
                categoryOptions.Parent,
                categoryOptions.Target,
                categoryOptions.Category,
                x => x.PlannedGrossProfit,
                (x, y) =>
                {
                    x.PlannedGrossProfit = y;
                    categoryOptions.NotifyPropertyChanged(nameof(x.PlannedGrossProfit));
                },
                "Плановая валовая прибыль",
                CategoryOptionInheritanceMode.InheritableToCategory,
                isEditable: WebClient.IsOperationAllowed(BusinessOperation.CategoryKpiEdit),
                isVisible: WebClient.IsOperationAllowed(BusinessOperation.CategoryKpiView) || WebClient.IsOperationAllowed(BusinessOperation.CategoryKpiEdit));

            categoryOptions.PlannedProfitOptionModel = new CategoryOptionViewModel<decimal?>(
                categoryOptions.Parent,
                categoryOptions.Target,
                categoryOptions.Category,
                x => x.PlannedProfit,
                (x, y) =>
                {
                    x.PlannedProfit = y;
                    categoryOptions.NotifyPropertyChanged(nameof(x.PlannedProfit));
                },
                "Плановая рентабельность",
                CategoryOptionInheritanceMode.InheritableToCategory,
                isEditable: WebClient.IsOperationAllowed(BusinessOperation.CategoryKpiEdit),
                isVisible: WebClient.IsOperationAllowed(BusinessOperation.CategoryKpiView) || WebClient.IsOperationAllowed(BusinessOperation.CategoryKpiEdit));

            categoryOptions.PlannedQuantityOptionModel = new CategoryOptionViewModel<decimal?>(
                categoryOptions.Parent,
                categoryOptions.Target,
                categoryOptions.Category,
                x => x.PlannedQuantity,
                (x, y) =>
                {
                    x.PlannedQuantity = y;
                    categoryOptions.NotifyPropertyChanged(nameof(x.PlannedQuantity));
                },
                "Плановое количество",
                CategoryOptionInheritanceMode.InheritableToCategory,
                isEditable: WebClient.IsOperationAllowed(BusinessOperation.CategoryKpiEdit),
                isVisible: WebClient.IsOperationAllowed(BusinessOperation.CategoryKpiView) || WebClient.IsOperationAllowed(BusinessOperation.CategoryKpiEdit));

            categoryOptions.PurchasePercentOptionModel = new CategoryOptionViewModel<decimal?>(
                categoryOptions.Parent,
                categoryOptions.Target,
                categoryOptions.Category,
                x => x.PurchasePercent,
                (x, y) =>
                {
                    x.PurchasePercent = y;
                    categoryOptions.NotifyPropertyChanged(nameof(x.PurchasePercent));
                },
                "Товары под закупку, %",
                CategoryOptionInheritanceMode.InheritableToCategory,
                isEditable: WebClient.IsOperationAllowed(BusinessOperation.CategoryKpiEdit),
                isVisible: WebClient.IsOperationAllowed(BusinessOperation.CategoryKpiView) || WebClient.IsOperationAllowed(BusinessOperation.CategoryKpiEdit));

            categoryOptions.BountyPercentOptionModel = new CategoryOptionViewModel<decimal?>(
                categoryOptions.Parent,
                categoryOptions.Target,
                categoryOptions.Category,
                x => x.BountyPercent,
                (x, y) =>
                {
                    x.BountyPercent = y;
                    categoryOptions.NotifyPropertyChanged(nameof(x.BountyPercent));
                },
                "Премия, %",
                CategoryOptionInheritanceMode.InheritableToCategory,
                isEditable: WebClient.IsOperationAllowed(BusinessOperation.CategoryKpiEdit),
                isVisible: WebClient.IsOperationAllowed(BusinessOperation.CategoryKpiView) || WebClient.IsOperationAllowed(BusinessOperation.CategoryKpiEdit));

            categoryOptions.PrintMarkersOptionModel = new CategoryOptionViewModel<bool>(
                categoryOptions.Parent,
                categoryOptions.Target,
                categoryOptions.Category,
                x => x.PrintMarkers,
                (x, y) =>
                {
                    x.PrintMarkers = y;
                    categoryOptions.NotifyPropertyChanged(nameof(x.PrintMarkers));
                },
                "Печатать этикетки",
                CategoryOptionInheritanceMode.InheritableToCategory,
                isEditable: WebClient.IsOperationAllowed(BusinessOperation.CategoryPrintMarkersEdit));

            categoryOptions.MarkerManufactureOptionModel = new CategoryOptionViewModel<string>(
                categoryOptions.Parent,
                categoryOptions.Target,
                categoryOptions.Category,
                x => x.MarkerManufacture,
                (x, y) =>
                {
                    x.MarkerManufacture = y;
                    categoryOptions.NotifyPropertyChanged(nameof(x.MarkerManufacture));
                },
                "Название компании",
                CategoryOptionInheritanceMode.InheritableToCategory,
                isEditable: WebClient.IsOperationAllowed(BusinessOperation.CategoryPrintMarkersEdit));

            categoryOptions.MarkerManufactureAddressOptionModel = new CategoryOptionViewModel<string>(
                categoryOptions.Parent,
                categoryOptions.Target,
                categoryOptions.Category,
                x => x.MarkerManufactureAddress,
                (x, y) =>
                {
                    x.MarkerManufactureAddress = y;
                    categoryOptions.NotifyPropertyChanged(nameof(x.MarkerManufactureAddress));
                },
                "Адрес",
                CategoryOptionInheritanceMode.InheritableToCategory,
                isEditable: WebClient.IsOperationAllowed(BusinessOperation.CategoryPrintMarkersEdit));

            categoryOptions.FeatureIdOptionModel = new CategoryOptionViewModel<int?>(
                categoryOptions.Parent,
                categoryOptions.Target,
                categoryOptions.Category,
                x => x.FeatureId,
                (x, y) =>
                {
                   x.FeatureId = y;
                   categoryOptions.NotifyPropertyChanged(nameof(x.FeatureId));
                },
                "Характеристики",
                CategoryOptionInheritanceMode.InheritableToCategory,
                x => categoryOptions.Features.FirstOrDefault(y => y.Id == x)?.Name,
                isEditable: WebClient.IsOperationAllowed(BusinessOperation.CategoryPrintMarkersEdit));
        }
    }
}