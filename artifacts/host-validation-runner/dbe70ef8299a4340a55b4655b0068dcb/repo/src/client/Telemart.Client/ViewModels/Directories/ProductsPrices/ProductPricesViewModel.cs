using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using AutoMapper;
using DevExpress.Data.Filtering;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Mvvm.POCO;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Grid;
using DevExpress.XtraPrinting;
using Grpc.Core;
using Humanizer;
using Microsoft.Extensions.Logging;
using Telemart.Client.Business.CategoryFilters;
using Telemart.Client.Business.PriceConversion;
using Telemart.Client.Common;
using Telemart.Client.Common.Layouts;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Utils;
using Telemart.Client.Common.Utils.Import;
using Telemart.Client.Controls.Accordion;
using Telemart.Client.Core.Helpers;
using Telemart.Client.Data.Requests.Features.Catalog;
using Telemart.Client.Data.Requests.Features.Category;
using Telemart.Client.Data.Requests.Features.ConstantQueries;
using Telemart.Client.Data.Requests.Features.Contractor;
using Telemart.Client.Data.Requests.Features.Currency;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.Requests.Features.Payments;
using Telemart.Client.Data.Requests.Features.Segment;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Helpers;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.TransferObjects.ParserRequests;
using Telemart.Client.TransferObjects.ParserSettings;
using Telemart.Client.TransferObjects.Prices;
using Telemart.Client.TransferObjects.Segment;
using Telemart.Client.Validators;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Common;
using Telemart.Client.ViewModels.Directories.Category;
using Telemart.Client.ViewModels.Directories.ProductsPrices.Exceptions;
using Telemart.Client.ViewModels.Parser.Dictionary;
using Telemart.Client.ViewModels.RobotProperties;
using Telemart.Client.ViewModels.Store.Order;
using Telemart.Client.ViewModels.Store.Order.ProductInformation;
using Telemart.Client.ViewModels.Validation;
using Telemart.Client.WebClient.Jobs;
using Telemart.Client.WebClient.Prices;
using Telemart.Common.ErrorHandling;
using Telemart.Common.PriceConversion;
using Telemart.Common.TransferObjects;
using Telemart.PriceCalculation;
using Telemart.PriceCalculation.Context;
using Telemart.PriceCalculation.Exceptions;

namespace Telemart.Client.ViewModels.Directories.ProductsPrices
{
    public sealed class ProductPricesViewModel : TelemartViewModelBase, IDataErrorInfo, ISupportHotkeys
    {
        private readonly ProductPricesRobotViewModel _robotViewModel;
        private readonly Lazy<List<object>> _priceInFilterItemsLazy;

        private IReadOnlyDictionary<int, decimal> _conversationRates;
        private IReadOnlyDictionary<int, string> _categoryNames;
        private IReadOnlyDictionary<int, string> _segmentToolTips;
        private Dictionary<int, (string Script, string Paremeters)> _robotScriptByCategory;
        private IPriceConverter _priceConverter;

        public ProductPricesViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            IRobotCalculator robotCalculator,
            IPriceConverterFactory priceConverterFactory,
            IParserClient parserClient,
            IExcelImportSettingsEngine<ProductSaveDto, ParserSettingsDto> excelPricesImportEngine,
            IPricesClient pricesClient,
            IModuleLayoutService moduleLayoutService,
            ProductInformationViewModel productInformationViewModel,
            IParserProductPriceValidator parserProductPriceValidator)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Mapper = mapper;
            RobotCalculator = robotCalculator;
            ModuleLayoutService = moduleLayoutService;
            PricesClient = pricesClient;
            PriceConverterFactory = priceConverterFactory;
            ExcelPricesImportEngine = excelPricesImportEngine;
            ParserClient = parserClient;
            ParserProductPriceValidator = parserProductPriceValidator;

            RefreshCommand = new AsyncCommand(RefreshAsync);
            HandleShowFilterPopupCommand = new DelegateCommand<FilterPopupEventArgs>(HandleShowFilterPopup);
            HandlePreviewKeyDownCommand = new DelegateCommand<KeyEventArgs>(HandlePreviewKeyDown);
            HandlePreviewTextInputCommand = new DelegateCommand<TextCompositionEventArgs>(HandlePreviewTextInput);
            HandleCategoriesPreviewKeyDownCommand = new DelegateCommand<KeyEventArgs>(HandleCategoriesPreviewKeyDown);
            HandleCustomColumnSortCommand = new DelegateCommand<CustomColumnSortEventArgs>(HandleCustomColumnSort);
            SetDisplayCurrencyModeCommand = new DelegateCommand<DisplayCurrencyMode>(SetDisplayCurrencyMode);
            SaveCommand = new AsyncCommand(SaveAsync);
            ShowCalcPricesDialogCommand = new DelegateCommand(ShowCalcPricesDialog, () => CurrentProduct != null);
            CalcCurrentProductPricesCommand = new AsyncCommand(OldCalcCurrentProductPricesAsync, () => CurrentProduct != null);
            OldCalcPricesCommand = new AsyncCommand(OldCalcPricesAsync);
            CalcPricesCommand = new AsyncCommand<bool>(CalcPricesAsync, _ => Products?.Any() == true);
            ShowRrpPriceViolatorsCommand = new AsyncCommand(ShowRrpPriceViolatorsAsync);
            SearchByFilterCommand = new AsyncCommand(SearchByFilterAsync);
            CancelFilterCommand = new DelegateCommand(CancelFilter);
            ParseExcelPriceCommand = new AsyncCommand(ParseExcelPriceAsync);
            ShowPropertyChangesCommand = new DelegateCommand(ShowPropertyChanges, () => CurrentProduct?.PropertyChanges?.Any() == true);
            RobotCommand = new DelegateCommand(Robot, () => SelectedCategory != null);

            HandleTableViewLoadedCommand = new DelegateCommand<RoutedEventArgs>(HandleTableViewLoaded);
            ExportCommand = new DelegateCommand<TableView>(Export);

            _robotViewModel = new ProductPricesRobotViewModel(WebClient, Dictionaries, MessageFacadeService);
            _robotViewModel.SetParentViewModel(this);

            ProductInformation = productInformationViewModel;

            FiltersLoader = new CategoryFiltersLoader(WebClient);

            _priceInFilterItemsLazy = new Lazy<List<object>>(() => new List<object>
            {
                new CustomComboBoxItem { DisplayValue = "(Все)", EditValue = new CustomComboBoxItem() },
                new CustomComboBoxItem { DisplayValue = "Есть значение", EditValue = CriteriaOperator.Parse($"[{GetPriceInFieldName()}] > 0") },
                new CustomComboBoxItem { DisplayValue = "Нет значения", EditValue = CriteriaOperator.Parse($"[{GetPriceInFieldName()}] <= 0") }
            });
        }

        public ProductPricesViewModel()
        {
        }

        #region Commands

        public IDelegateCommand ShowPropertyChangesCommand { get; }

        public IDelegateCommand RobotCommand { get; }

        public IDelegateCommand HandleCustomColumnSortCommand { get; }

        public IAsyncCommand RefreshCommand { get; }

        public IDelegateCommand HandleShowFilterPopupCommand { get; }

        public IModuleLayoutService ModuleLayoutService { get; }

        public IDelegateCommand HandlePreviewKeyDownCommand { get; }

        public IDelegateCommand HandlePreviewTextInputCommand { get; }

        public IDelegateCommand HandleCategoriesPreviewKeyDownCommand { get; }

        public IDelegateCommand SetDisplayCurrencyModeCommand { get; }

        public IAsyncCommand SaveCommand { get; }

        public IDelegateCommand ShowCalcPricesDialogCommand { get; }

        public IAsyncCommand CalcCurrentProductPricesCommand { get; }

        public IAsyncCommand OldCalcPricesCommand { get; }

        public IAsyncCommand CalcPricesCommand { get; }

        public IAsyncCommand ShowRrpPriceViolatorsCommand { get; }

        public IAsyncCommand SearchByFilterCommand { get; }

        public IDelegateCommand ExportCommand { get; }

        public IDelegateCommand HandleTableViewLoadedCommand { get; }

        public IDelegateCommand CancelFilterCommand { get; }

        public IAsyncCommand ParseExcelPriceCommand { get; }

        #endregion

        #region INPC

        public string SearchName
        {
            get { return GetProperty(() => SearchName); }
            set { SetProperty(() => SearchName, value); }
        }

        public ObservableRangeCollection<ComboBoxItem> AvailTypes
        {
            get { return GetProperty(() => AvailTypes); }
            private set { SetProperty(() => AvailTypes, value); }
        }

        public ObservableRangeCollection<AbcType> AbcClasses
        {
            get { return GetProperty(() => AbcClasses); }
            private set { SetProperty(() => AbcClasses, value); }
        }

        public ObservableCollection<TagColor> TagColors
        {
            get { return GetProperty(() => TagColors); }
            set { SetProperty(() => TagColors, value); }
        }

        public ObservableRangeCollection<ShowcasePickupModeType> ShowcasePickupModeTypes
        {
            get { return GetProperty(() => ShowcasePickupModeTypes); }
            private set { SetProperty(() => ShowcasePickupModeTypes, value); }
        }

        public ObservableRangeCollection<ComboBoxItem> ProductTypes
        {
            get { return GetProperty(() => ProductTypes); }
            private set { SetProperty(() => ProductTypes, value); }
        }

        public ObservableCollection<ComboBoxItem> SelectedProductTypes
        {
            get { return GetProperty(() => SelectedProductTypes); }
            set { SetProperty(() => SelectedProductTypes, value); }
        }

        public ObservableCollection<ComboBoxItem> SelectedAvailTypes
        {
            get { return GetProperty(() => SelectedAvailTypes); }
            set { SetProperty(() => SelectedAvailTypes, value); }
        }

        public ObservableRangeCollection<ComboBoxItem> Contractors
        {
            get { return GetProperty(() => Contractors); }
            private set { SetProperty(() => Contractors, value); }
        }

        public ReadOnlyObservableCollection<BonusType> BonusTypes
        {
            get { return GetProperty(() => BonusTypes); }
            private set { SetProperty(() => BonusTypes, value); }
        }

        public int MinPartialPayCount
        {
            get { return GetProperty(() => MinPartialPayCount); }
            set { SetProperty(() => MinPartialPayCount, value); }
        }

        public IReadOnlyCollection<CreditOfferDto> CreditOffers
        {
            get { return GetProperty(() => CreditOffers); }
            set { SetProperty(() => CreditOffers, value); }
        }

        public ObservableCollection<ComboBoxItem> SelectedContractors
        {
            get { return GetProperty(() => SelectedContractors); }
            set { SetProperty(() => SelectedContractors, value); }
        }

        public ObservableRangeCollection<CategoryViewItem> Categories
        {
            get { return GetProperty(() => Categories); }
            private set { SetProperty(() => Categories, value); }
        }

        public bool IsColumnChooserVisible
        {
            get { return GetProperty(() => IsColumnChooserVisible); }
            set { SetProperty(() => IsColumnChooserVisible, value); }
        }

        public ProductInformationViewModel ProductInformation
        {
            get { return GetProperty(() => ProductInformation); }
            private set { SetProperty(() => ProductInformation, value); }
        }

        public CategoryViewItem SelectedCategory
        {
            get { return GetProperty(() => SelectedCategory); }
            set { SetProperty(() => SelectedCategory, value, SelectedCategoryChanged); }
        }

        public ObservableCollection<RootAccordionItem> FilterItems
        {
            get { return GetProperty(() => FilterItems); }
            private set { SetProperty(() => FilterItems, value); }
        }

        public ObservableRangeCollection<ProductPriceViewItem> Products
        {
            get { return GetProperty(() => Products); }
            private set { SetProperty(() => Products, value); }
        }

        public ProductPriceViewItem CurrentProduct
        {
            get { return GetProperty(() => CurrentProduct); }
            set { SetProperty(() => CurrentProduct, value, CurrentProductChangedCallback); }
        }

        public ColumnBase CurrentColumn
        {
            get { return GetProperty(() => CurrentColumn); }
            set { SetProperty(() => CurrentColumn, value); }
        }

        public DisplayCurrencyMode DisplayCurrencyMode
        {
            get { return GetProperty(() => DisplayCurrencyMode); }
            set { SetProperty(() => DisplayCurrencyMode, value); }
        }

        public ReadOnlyObservableCollection<ProductAvailability> ProductAvailabilities
        {
            get { return GetProperty(() => ProductAvailabilities); }
            private set { SetProperty(() => ProductAvailabilities, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Employees
        {
            get { return GetProperty(() => Employees); }
            private set { SetProperty(() => Employees, value); }
        }

        public int RightPanelSelectedTabIndex
        {
            get { return GetProperty(() => RightPanelSelectedTabIndex); }
            set { SetProperty(() => RightPanelSelectedTabIndex, value); }
        }

        public string SalesCurrentMonthHeader
        {
            get { return GetProperty(() => SalesCurrentMonthHeader); }
            private set { SetProperty(() => SalesCurrentMonthHeader, value); }
        }

        public string SalesCurrentMonthHeaderToolTip
        {
            get { return GetProperty(() => SalesCurrentMonthHeaderToolTip); }
            private set { SetProperty(() => SalesCurrentMonthHeaderToolTip, value); }
        }

        public string SalesLastMonthHeader
        {
            get { return GetProperty(() => SalesLastMonthHeader); }
            private set { SetProperty(() => SalesLastMonthHeader, value); }
        }

        public string SalesLastMonthHeaderToolTip
        {
            get { return GetProperty(() => SalesLastMonthHeaderToolTip); }
            private set { SetProperty(() => SalesLastMonthHeaderToolTip, value); }
        }

        public string SalesBeforeLastMonthHeader
        {
            get { return GetProperty(() => SalesBeforeLastMonthHeader); }
            private set { SetProperty(() => SalesBeforeLastMonthHeader, value); }
        }

        public string SalesBeforeLastMonthHeaderToolTip
        {
            get { return GetProperty(() => SalesBeforeLastMonthHeaderToolTip); }
            private set { SetProperty(() => SalesBeforeLastMonthHeaderToolTip, value); }
        }

        public TableView TableView
        {
            get { return GetProperty(() => TableView); }
            set { SetProperty(() => TableView, value); }
        }

        #endregion

        public string Error => string.Empty;

        private IMapper Mapper { get; }

        private IPricesClient PricesClient { get; }

        private IParserClient ParserClient { get; }

        private IPriceConverterFactory PriceConverterFactory { get; }

        private IExcelImportSettingsEngine<ProductSaveDto, ParserSettingsDto> ExcelPricesImportEngine { get; }

        private IRobotCalculator RobotCalculator { get; }

        private CategoryFiltersLoader FiltersLoader { get; }

        private IParserProductPriceValidator ParserProductPriceValidator { get; }

        private IDocumentManagerService NonModalSizeableDialogDocumentManagerService => GetService<IDocumentManagerService>("NotModalSizeableDocumentManagerService");

        private ISaveFileDialogService SaveFileDialogService => GetService<ISaveFileDialogService>("ExcelSaveFileDialogService", ServiceSearchMode.PreferParents);

        private IDocumentManagerService DialogDocumentManagerService => GetService<IDocumentManagerService>("DialogDocumentManagerService", ServiceSearchMode.PreferParents);

        private IDocumentManagerService SizeableDialogDocumentManagerService => GetService<IDocumentManagerService>("SizeableDialogDocumentManagerService", ServiceSearchMode.PreferParents);

        private IDocumentManagerService NotModalSizeableDocumentManagerService => GetService<IDocumentManagerService>("NotModalSizeableDocumentManagerService", ServiceSearchMode.PreferParents);

        private IOpenFileDialogService OpenFileDialogService => GetService<IOpenFileDialogService>("ImportFromExcelFileDialogService");

        private IDocumentManagerService DocumentManagerService => GetService<IDocumentManagerService>();

        string IDataErrorInfo.this[string columnName] => IDataErrorInfoHelper.GetErrorText(this, columnName);

        public bool HandleHotkey(HotkeyMessage msg)
        {
            bool handled = false;

            if (msg.ModifierKeys == ModifierKeys.Control)
            {
                switch (msg.Key)
                {
                    case Key.E:
                        ExportCommand.Execute(TableView);
                        handled = true;
                        break;
                    case Key.S:
                        SaveCommand.Execute(null);
                        handled = true;
                        break;
                    case Key.F8:
                        CalcPricesCommand.Execute(null);
                        handled = true;
                        break;
                }
            }
            else
            {
                switch (msg.Key)
                {
                    case Key.F5:
                        RefreshCommand.Execute(null);
                        handled = true;
                        break;
                    case Key.F6:
                        IsColumnChooserVisible = !IsColumnChooserVisible;
                        handled = true;
                        break;
                    case Key.F8:
                        CalcPricesCommand.Execute(true);
                        handled = true;
                        break;
                    case Key.F9:
                        SetDisplayCurrencyModeCommand.Execute(DisplayCurrencyMode.Default);
                        handled = true;
                        break;
                    case Key.F10:
                        SetDisplayCurrencyModeCommand.Execute(DisplayCurrencyMode.Product);
                        handled = true;
                        break;
                    case Key.F11:
                        SetDisplayCurrencyModeCommand.Execute(DisplayCurrencyMode.Uah);
                        handled = true;
                        break;
                    case Key.F12:
                        SetDisplayCurrencyModeCommand.Execute(DisplayCurrencyMode.Usd);
                        handled = true;
                        break;
                }
            }

            return handled;
        }

        public async Task OldCalculatePricesAsync(
            IDocumentManagerService dialogDocumentManagerService,
            bool useRobotModeFromProduct,
            bool useScriptFromCategory,
            PriceRobotMode robotMode,
            string robotScript)
        {
            if (Products == null || !Products.Any())
            {
                MessageFacadeService.ShowNotificationWarning("Нечего вычислять");
                return;
            }

            ProgressScreenViewModel progressViewModel = new ProgressScreenViewModel("Обработка", Products.Count);

            using CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

            Task<int> task = OldCalculatePricesInternalAsync(
                Products,
                useScriptFromCategory,
                robotScript,
                useRobotModeFromProduct,
                robotMode,
                progressViewModel,
                cancellationTokenSource.Token);

            dialogDocumentManagerService.ShowView("ProgressScreenView", progressViewModel);

            if (!progressViewModel.IsOk)
            {
                cancellationTokenSource.Cancel();
            }

            try
            {
                int processedCount = await task;
                MessageFacadeService.ShowNotificationInfo($"{Message(processedCount)}");
            }
            catch (PriceCalculationCompilationException exception)
            {
                if (progressViewModel.ProcessedCount > 0)
                {
                    MessageFacadeService.ShowNotificationWarning($"Ошибка. {Message(progressViewModel.ProcessedCount)}");
                }

                MessageFacadeService.ShowMessageBox(exception.Message, "Ошибки при обработке скрипта", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (PriceCalculationValidationException exception)
            {
                if (progressViewModel.ProcessedCount > 0)
                {
                    MessageFacadeService.ShowNotificationWarning($"Ошибка. {Message(progressViewModel.ProcessedCount)}");
                }

                ShowValidationResultView($"Ошибки. Товар {exception.Args.ProductName} ({exception.Args.ProductId})", exception.Args.ErrorMessages);
            }
            catch (OperationCanceledException)
            {
                MessageFacadeService.ShowNotificationWarning($"Отмена. {Message(progressViewModel.ProcessedCount)}");
            }
            catch (Exception exception)
            {
                if (progressViewModel.ProcessedCount > 0)
                {
                    MessageFacadeService.ShowNotificationWarning($"Ошибка. {Message(progressViewModel.ProcessedCount)}");
                }

                MessageFacadeService.ShowMessageBox(exception.Message, "Ошибки при выполнении скрипта", MessageBoxButton.OK, MessageBoxImage.Error);

                Logger.LogError(exception, "Failed to calculate prices");
            }
        }

        public async Task<(string Debug, CalculatePriceResult Result)> DebugAsync(PriceRobotMode robotMode, string robotScript, bool applyResult)
        {
            try
            {
                Result fetchResult = await FetchFullDataAsync(new[] { CurrentProduct });

                if (!fetchResult.IsSuccess)
                {
                    return (null, null);
                }

                CalculatePriceContext context = CurrentProduct.GetContext(robotMode, CreditOffers, MinPartialPayCount);

                CalculatePriceResult result = await RobotCalculator.CalculateAsync(context, robotScript);

                if (applyResult && result.Success)
                {
                    IPriceConverter priceConverter = await PriceConverterFactory.CreateAsync();
                    CurrentProduct.SetCalculatePriceResult(Dictionaries, result.Data, priceConverter);
                }

                return (context.GetDebug(), result);
            }
            catch (PriceCalculationCompilationException exception)
            {
                MessageFacadeService.ShowMessageBox(exception.Message, "Ошибки при обработке скрипта", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to calculate price");

                MessageFacadeService.ShowMessageBox(exception.Message, "Ошибки при выполнении скрипта", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return (null, null);
        }

        public async ValueTask<(string Script, string Parameters)> GetRobotScriptByCategoryAsync(int categoryId)
        {
            (string Script, string Parameters) script;

            if (_robotScriptByCategory.TryGetValue(categoryId, out script))
            {
                return script;
            }

            var robotScript = await WebClient.ExecuteApiRequestAsync(new QueryRobotScript(categoryId));

            script = (robotScript?.Script, robotScript?.Parameters);

            _robotScriptByCategory[categoryId] = script;

            return script;
        }

        public void SetRobotScript(int categoryId, string script, string parameters)
        {
            _robotScriptByCategory[categoryId] = (script, parameters);
        }

        protected override async Task HandleLoadedAsync()
        {
            string currentMonth = DateToMonthStr(DateTime.Today);
            SalesCurrentMonthHeader = MonthToSalesHeader(currentMonth);
            SalesCurrentMonthHeaderToolTip = MonthToSalesHeaderTooltip(currentMonth);

            string lastMonth = DateToMonthStr(DateTime.Today.AddMonths(-1));
            SalesLastMonthHeader = MonthToSalesHeader(lastMonth);
            SalesLastMonthHeaderToolTip = MonthToSalesHeaderTooltip(lastMonth);

            string beforeLastMonth = DateToMonthStr(DateTime.Today.AddMonths(-2));
            SalesBeforeLastMonthHeader = MonthToSalesHeader(beforeLastMonth);
            SalesBeforeLastMonthHeaderToolTip = MonthToSalesHeaderTooltip(beforeLastMonth);

            ProductAvailabilities = Dictionaries.GetItems<ProductAvailability>()
                .Where(x => x.Active)
                .ToReadOnlyObservableCollection();

            AvailTypes = Dictionaries.GetItems<ProductAvailability>()
                .Where(x => x.Active)
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToObservableRangeCollection();

            AbcClasses = Dictionaries.GetItems<AbcType>().ToObservableRangeCollection();

            TagColors = Dictionaries.GetItems<TagColor>()
                .Where(x => x.Active)
                .ToObservableRangeCollection();

            ShowcasePickupModeTypes = Dictionaries.GetItems<ShowcasePickupModeType>().ToObservableRangeCollection();
            ProductTypes = Dictionaries.GetItems<ProductType>()
                .Where(x => !x.IsVirtual)
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToObservableRangeCollection();

            BonusTypes = Dictionaries.GetItems<BonusType>().ToReadOnlyObservableCollection();

            DisplayCurrencyMode = DisplayCurrencyMode.Default;

            CreditOffers = await WebClient.ExecuteApiRequestAsync(new QueryCreditOffers());

            object minPartialPayCountObj = await WebClient.ExecuteApiRequestAsync(new QueryConstant(ConstantKeys.MinPartialPayCount));

            if (int.TryParse(minPartialPayCountObj.ToString(), out int minPartialPayCount))
            {
                MinPartialPayCount = minPartialPayCount;
            }
            else
            {
                MinPartialPayCount = 0;
            }

            Task<List<CategoryDto>> categoriesTask = WebClient.ExecuteApiRequestAsync(new QueryCategories(), true).GetPagedResultDataAsync();
            Task<List<ContractorDto>> contractorsTask = WebClient.ExecuteApiRequestAsync(new QueryContractors(), true).GetPagedResultDataAsync();
            Task<List<EmployeeDto>> employeesTask = WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true).GetPagedResultDataAsync();
            Task robotTask = RobotCalculator.WarmUpAsync();

            await Task.WhenAll(categoriesTask, contractorsTask, employeesTask, robotTask);

            List<CategoryDto> categories = categoriesTask.Result;

            _categoryNames = categories.ToDictionary(x => x.Id, x => !string.IsNullOrWhiteSpace(x.NameFull) ? x.NameFull : x.Name);

            if (WebClient.IsOperationAllowed(BusinessOperation.Intern))
            {
                categories = categories.Where(x => x.Active > 0 && WebClient.AuthenticatedEmployee.AllowCategories.Contains(x.Id)).ToList();
            }
            else
            {
                categories = categories.Where(x => x.Active > 0).ToList();
            }

            Categories = categories.OrderBy(x => x.Position)
                .Select(x => Mapper.Map<CategoryViewItem>(x))
                .ToObservableRangeCollection();

            Contractors = contractorsTask.Result
                .Where(x => x.Active && !x.IsFolder && (x.IsSupplier || x.IsCompetitor))
                .OrderByDescending(x => x.IsSupplier)
                .ThenBy(x => x.Name)
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToObservableRangeCollection();

            Employees = employeesTask.Result
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToReadOnlyObservableCollection();

            SelectedAvailTypes = AvailTypes.Where(x => x.Id != ProductAvailability.Archive.Id).ToObservableRangeCollection();

            ModuleLayoutService.Init(Module.ProductPricesId, this);

            return;

            static string DateToMonthStr(DateTime dateTime)
            {
                return CultureInfo.CurrentUICulture.DateTimeFormat.GetMonthName(dateTime.Month);
            }

            static string MonthToSalesHeader(string month)
            {
                return month.Substring(0, 1).ToUpperInvariant();
            }

            static string MonthToSalesHeaderTooltip(string month)
            {
                return $"Продажи за {month.ToLowerInvariant()}";
            }
        }

        private static string GetPriceInFieldName()
        {
            return $"{nameof(ProductPriceViewItem.PriceInPriceData)}.{nameof(ProductPriceDataViewItem.DisplayPriceOld)}";
        }

        private static string Message(int n)
        {
            string action = WordEndingHelper.GetWordByNumber(n, new[] { "обработана", "обработано", "обработано" });
            string items = WordEndingHelper.GetWordByNumber(n, new[] { "строка", "строки", "строк" });

            return $"{action} {n} {items}".Transform(To.SentenceCase);
        }

        private static int? GetDisplayCurrency(DisplayCurrencyMode mode, int productCurrencyId)
        {
            int? displayCurrency = mode switch
            {
                DisplayCurrencyMode.Default => null,
                DisplayCurrencyMode.Product => productCurrencyId,
                DisplayCurrencyMode.Uah => Currency.Uah.Id,
                DisplayCurrencyMode.Usd => Currency.Usd.Id,
                DisplayCurrencyMode.Eur => Currency.Eur.Id,
                _ => throw new NotSupportedException()
            };

            return displayCurrency;
        }

        private static ProductPriceDto MapViewItemToDto(ProductPriceViewItem viewItem)
        {
            return new ProductPriceDto()
            {
                Id = viewItem.Id,
                ProductTypeId = viewItem.ProductTypeId,
                Name = viewItem.Name,
                Density1Month = viewItem.Density1Month,
                Density14Days = viewItem.Density14Days,
                DaysInStock = viewItem.DaysInStock,
                InStock = viewItem.InStock,
                Preorder = viewItem.Preorder,
                DaysFromLastSale = viewItem.DaysFromLastSale,
                Mining = viewItem.Mining,
                FreeDelivery = viewItem.FreeDelivery,
                MaxTradeInPrice = viewItem.MaxTradeInPrice,
                WarrantyRetail = viewItem.WarrantyRetail,
                CategoryId = viewItem.CategoryId,
                LabelWholesaleId = viewItem.LabelWholesale?.Id,
                AvailId = viewItem.AvailOld.Id,
                UsdCurrency = viewItem.UsdCurrency,
                Visits = viewItem.Visits,
                Hotline = viewItem.Hotline,
                MinLeftover = viewItem.MinLeftover,
                PlannedLeftover = viewItem.PlannedLeftover,
                Conversion = viewItem.Conversion,
                Rozetka = viewItem.Rozetka,
                Monomarket = viewItem.Monomarket,
                ShowcasesStock = viewItem.ShowcasesStock,
                ShowcasesCapacity = viewItem.ShowcasesCapacity,
                F2Markup = viewItem.F2Markup,
                MinProductMarginPercent = viewItem.MinProductMarginPercent,
                MinCategoryMarginPercent = viewItem.MinCategoryMarginPercent,
                PlannedMarkup = viewItem.PlannedMarkup,
                ShowcasePickupModeId = viewItem.ShowcasePickupModeId,
                UsePlannedMarkupInAutoShowcase = viewItem.UsePlannedMarkupInAutoShowcase,
                ReservedQuantity = viewItem.ReservedQuantity,
                OrdersLastMonth = viewItem.OrdersLastMonth,
                OrdersCurrentMonth = viewItem.OrdersCurrentMonth,
                OrdersBeforeLastMonth = viewItem.OrdersBeforeLastMonth,
                BonusTypeId = viewItem.BonusTypeId,
                BonusesToCharge = viewItem.BonusesToCharge,
                RobotModeAutoId = viewItem.RobotModeAuto.Id,
                CategoryRobotModeAutoId = viewItem.CategoryRobotModeAuto.Id,
                CategoryRobotModeManualId = viewItem.RobotModeManual.Id,
                LastSalePriceUsd = viewItem.LastSalePriceUsd,
                LabelRetailId = viewItem.LabelRetailOld?.Id,
                HotlinePosition = viewItem.HotlinePosition,
                NotSavedPrice1Usd = viewItem.Telemart1PriceData.GetPriceUsdValue(),
                PriceWarehouseUsd = (decimal)viewItem.WarehousePriceData.GetPriceUsdValue(),
                SalesCurrentMonth = viewItem.SalesCurrentMonth,
                SalesLastMonth = viewItem.SalesLastMonth,
                RobotModeManualId = viewItem.RobotModeManual.Id,
                PriceTransitUsd = viewItem.PriceTransit,
                HotlinePriceUsd = viewItem.HotlinePriceUsd,
                HotlineMinPriceUsd = viewItem.HotlineMinPriceUsd,
                ShowInAccessories = viewItem.ShowInAccessories,
                Prices = new[]
                {
                    new ProductPriceSimpleDto()
                    {
                        TagColorId = viewItem.Telemart1PriceData.GetTagColor().Id,
                        PriceTypeId = viewItem.Telemart1PriceData.GetPriceKind().Id,
                        ProductId = viewItem.Id,
                        Price = viewItem.Telemart1PriceData.GetPriceValue(),
                        PricePrev = viewItem.Telemart1PriceData.GetPricePrevValue(),
                        MaxBonusesToUse = viewItem.Telemart1PriceData.MaxBonusesToUse,
                        PartialPay = viewItem.Telemart1PriceData.GetPartialPay(),
                        PartialPayPb = viewItem.Telemart1PriceData.GetPartialPayPb(),
                        PartialPayPumb = viewItem.Telemart1PriceData.GetPartialPayPumb(),
                        PartialPayAb = viewItem.Telemart1PriceData.GetPartialPayAb(),
                        CurrencyId = viewItem.Telemart1PriceData.DisplayCurrencyId
                    },
                    new ProductPriceSimpleDto()
                    {
                        TagColorId = viewItem.Telemart2PriceData.GetTagColor().Id,
                        PriceTypeId = viewItem.Telemart2PriceData.GetPriceKind().Id,
                        ProductId = viewItem.Id,
                        Price = viewItem.Telemart2PriceData.GetPriceValue(),
                        PricePrev = viewItem.Telemart2PriceData.GetPricePrevValue(),
                        MaxBonusesToUse = viewItem.Telemart2PriceData.MaxBonusesToUse,
                        PartialPay = viewItem.Telemart2PriceData.GetPartialPay(),
                        PartialPayPb = viewItem.Telemart2PriceData.GetPartialPayPb(),
                        CurrencyId = viewItem.Telemart2PriceData.DisplayCurrencyId
                    },
                    new ProductPriceSimpleDto()
                    {
                        TagColorId = viewItem.Telemart3PriceData.GetTagColor().Id,
                        PriceTypeId = viewItem.Telemart3PriceData.GetPriceKind().Id,
                        ProductId = viewItem.Id,
                        Price = viewItem.Telemart3PriceData.GetPriceValue(),
                        PricePrev = viewItem.Telemart3PriceData.GetPricePrevValue(),
                        MaxBonusesToUse = viewItem.Telemart3PriceData.MaxBonusesToUse,
                        PartialPay = viewItem.Telemart3PriceData.GetPartialPay(),
                        PartialPayPb = viewItem.Telemart3PriceData.GetPartialPayPb(),
                        CurrencyId = viewItem.Telemart3PriceData.DisplayCurrencyId
                    },
                    new ProductPriceSimpleDto()
                    {
                        TagColorId = viewItem.Telemart4PriceData.GetTagColor().Id,
                        PriceTypeId = viewItem.Telemart4PriceData.GetPriceKind().Id,
                        ProductId = viewItem.Id,
                        Price = viewItem.Telemart4PriceData.GetPriceValue(),
                        PricePrev = viewItem.Telemart4PriceData.GetPricePrevValue(),
                        MaxBonusesToUse = viewItem.Telemart4PriceData.MaxBonusesToUse,
                        PartialPay = viewItem.Telemart4PriceData.GetPartialPay(),
                        PartialPayPb = viewItem.Telemart4PriceData.GetPartialPayPb(),
                        CurrencyId = viewItem.Telemart4PriceData.DisplayCurrencyId
                    },
                    new ProductPriceSimpleDto()
                    {
                        TagColorId = viewItem.Telemart5PriceData.GetTagColor().Id,
                        PriceTypeId = viewItem.Telemart5PriceData.GetPriceKind().Id,
                        ProductId = viewItem.Id,
                        Price = viewItem.Telemart5PriceData.GetPriceValue(),
                        PricePrev = viewItem.Telemart5PriceData.GetPricePrevValue(),
                        MaxBonusesToUse = viewItem.Telemart5PriceData.MaxBonusesToUse,
                        PartialPay = viewItem.Telemart5PriceData.GetPartialPay(),
                        PartialPayPb = viewItem.Telemart5PriceData.GetPartialPayPb(),
                        CurrencyId = viewItem.Telemart5PriceData.DisplayCurrencyId
                    }
                },
                PromoHistory = viewItem.PromoHistory,
                Sales = viewItem.Sales,
                Purchases = viewItem.Purchases,
                HotlineAbcClassPrices = viewItem.HotlineAbcClassPrices,
                SupplierPrices = viewItem.SupplierPrices.Select(x => new ProductContractorPriceDto()
                {
                    ContractorId = x.ContractorId,
                    AllowDocuments = x.AllowDocuments,
                    ContractorName = x.ContractorName,
                    PriceUsd = x.PriceUsd,
                    AbcId = x.AbcTypeId,
                    CreatedOn = x.CreatedOn
                }).ToArray(),
                RrpPrices = viewItem.RrpPrices.Select(x => new ProductContractorPriceDto()
                {
                    ContractorId = x.ContractorId,
                    AllowDocuments = x.AllowDocuments,
                    ContractorName = x.ContractorName,
                    PriceUsd = x.PriceUsd,
                    AbcId = x.AbcTypeId,
                    CreatedOn = x.CreatedOn
                }).ToArray(),
                CompetitorPrices = viewItem.CompetitorPrices.Select(x => new ProductContractorPriceDto()
                {
                    ContractorId = x.ContractorId,
                    AllowDocuments = x.AllowDocuments,
                    ContractorName = x.ContractorName,
                    PriceUsd = x.PriceUsd,
                    AbcId = x.AbcTypeId,
                    CreatedOn = x.CreatedOn
                }).ToArray(),
                ConfiguratorPrices = viewItem.ConfiguratorPrices.Select(x => new ProductContractorPriceDto()
                {
                    ContractorId = x.ContractorId,
                    AllowDocuments = x.AllowDocuments,
                    ContractorName = x.ContractorName,
                    PriceUsd = x.PriceUsd,
                    AbcId = x.AbcTypeId,
                    CreatedOn = x.CreatedOn
                }).ToArray(),
                SearchTemplatePrices = viewItem.SearchTemplateProducts.Select(x => new ProductSearchTemplatePriceDto()
                {
                    SearchTemplateId = x.SearchTemplateId,
                    SearchTemplateName = x.SearchTemplateName,
                    Price = x.Price,
                    ContractorId = x.ContractorId,
                    ContractorName = x.ContractorName
                }).ToArray(),
                BasedOnPrices = GetAssembledComputerRulePriceViewItems()
                    .Where(x => x is not null)
                    .Select(x => new ProductPriceSimpleDto()
                    {
                        TagColorId = x.GetTagColor().Id,
                        PriceTypeId = x.GetPriceKind().Id,
                        ProductId = viewItem.Id,
                        Price = (decimal)x.GetPriceUsdValue(),
                        MaxBonusesToUse = x.MaxBonusesToUse,
                        PartialPay = x.GetPartialPay(),
                        PartialPayPb = x.GetPartialPayPb()
                    })
                    .ToArray()
            };

            IEnumerable<ProductPriceDataViewItem> GetAssembledComputerRulePriceViewItems()
            {
                yield return viewItem.AssembledComputerRuleBasePrice1;
                yield return viewItem.AssembledComputerRuleBasePrice2;
                yield return viewItem.AssembledComputerRuleBasePrice3;
                yield return viewItem.AssembledComputerRuleBasePrice4;
                yield return viewItem.AssembledComputerRuleBasePrice5;
            }
        }

        private async Task<int> OldCalculatePricesInternalAsync(
            IReadOnlyCollection<ProductPriceViewItem> toProcess,
            bool useScriptFromCategory,
            string robotScript,
            bool useRobotModeFromProduct,
            PriceRobotMode robotMode,
            ProgressScreenViewModel progressViewModel,
            CancellationToken cancellationToken)
        {
            try
            {
                Result fetchResult = await FetchFullDataAsync(toProcess);

                if (!fetchResult.IsSuccess)
                {
                    return 0;
                }

                int i = 0;

                IPriceConverter priceConverter = await PriceConverterFactory.CreateAsync();

                foreach (ProductPriceViewItem product in toProcess)
                {
                    if (i % 100 == 0)
                    {
                        await Task.Delay(1, cancellationToken);
                    }

                    (string Script, string Parameters) productRobotScript = await GetRobotScriptByCategoryAsync(product.CategoryId);

                    string code = useScriptFromCategory
                        ? RobotHelper.GetFullScript(productRobotScript.Script, productRobotScript.Parameters)
                        : robotScript;

                    PriceRobotMode priceRobotMode = useRobotModeFromProduct
                        ? product.RobotModeManual
                        : robotMode;

                    CalculatePriceContext context = product.GetContext(priceRobotMode, CreditOffers, MinPartialPayCount);

                    if (!string.IsNullOrWhiteSpace(code))
                    {
                        CalculatePriceResult result = await RobotCalculator.CalculateAsync(context, code, cancellationToken);

                        if (!result.Success)
                        {
                            throw new PriceCalculationValidationException(product.Id, product.Name, result.Errors);
                        }

                        product.SetCalculatePriceResult(Dictionaries, result.Data, priceConverter);
                    }

                    progressViewModel.SetProcessedCount(++i);

                    cancellationToken.ThrowIfCancellationRequested();
                }

                return i;
            }
            catch (Exception)
            {
                progressViewModel.CancelCommand.Execute(null);
                throw;
            }
        }

        private void HandleCustomColumnSort(CustomColumnSortEventArgs e)
        {
        }

        private void HandleShowFilterPopup(FilterPopupEventArgs e)
        {
            if (string.Equals(e.Column.FieldName, GetPriceInFieldName(), StringComparison.Ordinal))
            {
                e.ComboBoxEdit.ItemsSource = _priceInFilterItemsLazy.Value;
                e.Handled = true;
            }
        }

        private void HandlePreviewKeyDown(KeyEventArgs e)
        {
            if (CurrentColumn != null && CurrentProduct != null)
            {
                switch (CurrentColumn.FieldName)
                {
                    case nameof(ProductPriceViewItem.LabelRetail):
                        {
                            int? number = KeyToNumber(e.Key);

                            if (number.HasValue)
                            {
                                CurrentProduct.LabelRetail = number.Value != 0
                                    ? (Dictionaries.GetItemById<ProductLabel>(number.Value) ?? CurrentProduct.LabelRetail)
                                    : null;

                                e.Handled = true;
                            }

                            break;
                        }

                    case nameof(ProductPriceViewItem.LabelWholesale):
                        {
                            int? number = KeyToNumber(e.Key);

                            if (number.HasValue)
                            {
                                CurrentProduct.LabelWholesale = number.Value != 0
                                    ? (Dictionaries.GetItemById<ProductLabel>(number.Value) ?? CurrentProduct.LabelWholesale)
                                    : null;

                                e.Handled = true;
                            }

                            break;
                        }

                    case nameof(ProductPriceViewItem.RobotModeManual):
                        {
                            PriceRobotMode robotMode = GetRobotModeById(KeyToNumber(e.Key) ?? 0);

                            if (robotMode != null)
                            {
                                CurrentProduct.RobotModeManual = robotMode;
                                e.Handled = true;
                            }

                            break;
                        }

                    case nameof(ProductPriceViewItem.RobotModeAuto):
                        {
                            PriceRobotMode robotMode = GetRobotModeById(KeyToNumber(e.Key) ?? 0);

                            if (robotMode != null)
                            {
                                CurrentProduct.RobotModeAuto = robotMode;
                                e.Handled = true;
                            }

                            break;
                        }

                    case nameof(ProductPriceViewItem.Hotline):
                        {
                            switch (e.Key)
                            {
                                case Key.Add:
                                case Key.OemPlus:
                                    CurrentProduct.Hotline = 1;
                                    e.Handled = true;
                                    break;
                                case Key.Subtract:
                                case Key.OemMinus:
                                    CurrentProduct.Hotline = 0;
                                    e.Handled = true;
                                    break;
                            }

                            break;
                        }

                    case nameof(ProductPriceViewItem.Mining):
                        {
                            switch (e.Key)
                            {
                                case Key.Add:
                                case Key.OemPlus:
                                    CurrentProduct.Mining = true;
                                    e.Handled = true;
                                    break;
                                case Key.Subtract:
                                case Key.OemMinus:
                                    CurrentProduct.Mining = false;
                                    e.Handled = true;
                                    break;
                            }

                            break;
                        }

                    case nameof(ProductPriceViewItem.Rozetka):
                        {
                            switch (e.Key)
                            {
                                case Key.Add:
                                case Key.OemPlus:
                                    CurrentProduct.Rozetka = 1;
                                    e.Handled = true;
                                    break;
                                case Key.Subtract:
                                case Key.OemMinus:
                                    CurrentProduct.Rozetka = 0;
                                    e.Handled = true;
                                    break;
                            }

                            break;
                        }

                    case nameof(ProductPriceViewItem.Monomarket):
                        {
                            switch (e.Key)
                            {
                                case Key.Add:
                                case Key.OemPlus:
                                    CurrentProduct.Monomarket = 1;
                                    e.Handled = true;
                                    break;
                                case Key.Subtract:
                                case Key.OemMinus:
                                    CurrentProduct.Monomarket = 0;
                                    e.Handled = true;
                                    break;
                            }

                            break;
                        }

                    case nameof(ProductPriceViewItem.FreeDelivery):
                        {
                            switch (e.Key)
                            {
                                case Key.Add:
                                case Key.OemPlus:
                                    CurrentProduct.FreeDelivery = true;
                                    e.Handled = true;
                                    break;
                                case Key.Subtract:
                                case Key.OemMinus:
                                    CurrentProduct.FreeDelivery = false;
                                    e.Handled = true;
                                    break;
                            }

                            break;
                        }

                    case nameof(ProductPriceViewItem.ShowInAccessories):
                        {
                            switch (e.Key)
                            {
                                case Key.Add:
                                case Key.OemPlus:
                                    CurrentProduct.ShowInAccessories = true;
                                    e.Handled = true;
                                    break;
                                case Key.Subtract:
                                case Key.OemMinus:
                                    CurrentProduct.ShowInAccessories = false;
                                    e.Handled = true;
                                    break;
                            }

                            break;
                        }

                    case nameof(ProductPriceViewItem.ShowcasePickupModeId):
                        {
                            switch (e.Key)
                            {
                                case Key.NumPad1: case Key.D1:
                                        CurrentProduct.ShowcasePickupModeId = ShowcasePickupModeType.Standart.Id;
                                        break;
                                case Key.NumPad0: case Key.D0:
                                        CurrentProduct.ShowcasePickupModeId = null;
                                        break;
                            }
                        }

                        break;
                }

                static int? KeyToNumber(Key key)
                {
                    int? value = null;

                    if (key >= Key.NumPad0 && key <= Key.NumPad9)
                    {
                        value = key - Key.NumPad0;
                    }
                    else if (key >= Key.D0 && key <= Key.D9)
                    {
                        value = key - Key.D0;
                    }

                    return value;
                }
            }
        }

        private void HandlePreviewTextInput(TextCompositionEventArgs e)
        {
            if (CurrentColumn != null && CurrentProduct != null)
            {
                switch (CurrentColumn.FieldName)
                {
                    case nameof(ProductPriceViewItem.Avail):
                        {
                            string text = e.Text;

                            ProductAvailability availability = int.TryParse(text, out int id)
                                ? ProductAvailabilities.FirstOrDefault(x => x.Id == id)
                                : ProductAvailabilities.FirstOrDefault(x => string.Equals(x.NameShort, text, StringComparison.OrdinalIgnoreCase));

                            if (availability != null)
                            {
                                CurrentProduct.Avail = availability;
                                CurrentProduct.AvailModifiedBy = WebClient.AuthenticatedEmployee.Id;
                                e.Handled = true;
                            }

                            break;
                        }
                }
            }
        }

        private void HandleCategoriesPreviewKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                RefreshCommand.Execute(null);
                e.Handled = true;
            }
        }

        private async Task RefreshAsync()
        {
            if (SelectedCategory == null)
            {
                MessageFacadeService.ShowNotificationWarning("Поле \"Категория\" не заполнено");
                return;
            }

            _robotScriptByCategory = new Dictionary<int, (string Script, string Paremeters)>();

            if (GetChangedProducts().Any() && !MessageFacadeService.Confirm("Вы уверены?"))
            {
                return;
            }

            if (FilterItems == null)
            {
                return;
            }

            _robotViewModel.CancelCommand.Execute(null);

            try
            {
                Products = null;

                List<int> filterIds = new List<int>();
                List<int> labelIds = new List<int>();
                List<int> warehouseIds = new List<int>();

                foreach (RootAccordionItem rootAccordionItem in FilterItems)
                {
                    switch (rootAccordionItem.Item)
                    {
                        case CheckedListAccordionItem list when list.SelectedItems?.Any() == true:
                            switch (list.Name)
                            {
                                case CategoryFiltersLoader.LabelItemName:
                                    labelIds.AddRange(list.SelectedItems);
                                    break;
                                case CategoryFiltersLoader.WarehouseItemName:
                                    warehouseIds.AddRange(list.SelectedItems);
                                    break;
                                default:
                                    filterIds.AddRange(list.SelectedItems);
                                    break;
                            }

                            break;
                    }
                }

                int[] contractorIds = SelectedContractors?.Select(x => x.Id).ToArray();

                QueryProductByFilter request = new QueryProductByFilter(
                    Constants.TelemartContractorId,
                    SelectedCategory.Id,
                    filterIds,
                    labelIds,
                    warehouseIds,
                    SelectedAvailTypes?.Select(x => x.Id).ToArray(),
                    null,
                    null,
                    null,
                    null,
                    false,
                    ProductSort.None,
                    true,
                    false,
                    false,
                    false,
                    0,
                    10000000,
                    false,
                    SearchName,
                    contractorIds,
                    productTypeIds: SelectedProductTypes?.Select(x => x.Id).ToArray(),
                    useElastic: false);

                Task<Result<ProductPricesDto>> productPricesResultTask = QueryProductPricesSimpleAsync(request);
                Task<IPriceConverter> priceConverterTask = PriceConverterFactory.CreateAsync();

                await Task.WhenAll(RefreshCurrenciesAsync(), RefreshSegmentsAsync(), productPricesResultTask, priceConverterTask);

                if (productPricesResultTask.Result?.Data is null || priceConverterTask.Result is null)
                {
                    return;
                }

                _priceConverter = priceConverterTask.Result;

                Products = productPricesResultTask.Result.Data.ProductPrices
                    .OrderBy(x => x.Name)
                    .Select(x => MapToViewItem(x, new ProductPriceViewItem(), _priceConverter))
                    .ToObservableRangeCollection();

                CurrentProduct = Products.FirstOrDefault();

                RightPanelSelectedTabIndex = 1;
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to load product prices");
                MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
            }
        }

        private async Task<Result<ProductPricesDto>> QueryProductPricesSimpleAsync(QueryProductByFilter filterRequest)
        {
            PagedResult<ProductDto> productsData = await WebClient.ExecuteCatalogApiRequestAsync(filterRequest);

            Result<ProductPricesDto> result = await PricesClient.QuerySimplePricesByIdsAsync(new QueryPricesByIdsRequest(productsData.Data.Select(x => x.Id).ToArray(), filterRequest.ContractorIds), this);

            return result;
        }

        private async Task<Result<ProductPricesDto>> QueryProductPricesAsync(int[] productIds, int[] contractorIds)
        {
            Result<ProductPricesDto> result = await PricesClient.QueryPricesByIdsAsync(new QueryPricesByIdsRequest(productIds, contractorIds), this);

            return result;
        }

        private async Task ParseExcelPriceAsync()
        {
            if (!WebClient.IsOperationAllowed(BusinessOperation.ParseExcelPrice))
            {
                MessageFacadeService.ShowNotificationError("У вас нет прав на выполнение операции");
                return;
            }

            PagedResult<ContractorDto> contractors = await WebClient.ExecuteApiRequestAsync(new QueryContractors(true, true));

            ICollection<ComboBoxItem> contractorsToSelect = contractors.Data
                .Where(x => x.ParserSettings?.Any(z => z.TypeId == ParserSettingsType.Excel.Id) == true)
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToArray();

            SelectItemParameter selectContractorParameter = new SelectItemParameter(contractorsToSelect, "Выбор контрагента", "Контрагент");

            SelectItemViewModel selectContractorViewModel = DialogDocumentManagerService.ShowView<SelectItemViewModel>(selectContractorParameter, this);

            if (!selectContractorViewModel.IsOk)
            {
                return;
            }

            ContractorDto contractor = contractors.Data.First(x => x.Id == selectContractorViewModel!.SelectedItem!.Value.Id);

            ParserSettingsDto[] parserSettings = contractor.ParserSettings.Where(x => x.TypeId == ParserSettingsType.Excel.Id).ToArray();

            if (!parserSettings.Any())
            {
                MessageFacadeService.ShowNotificationWarning("У контрагента отсутствует Excel парсер");
                return;
            }

            ParserSettingsDto parserSettingsDto;

            if (parserSettings.Length > 1)
            {
                ComboBoxItem[] parsers = contractor.ParserSettings.Where(x => x.TypeId == ParserSettingsType.Excel.Id).Select(x => new ComboBoxItem(x.Id, x.Name)).ToArray();

                SelectItemParameter selectParserParameter = new SelectItemParameter(parsers, "Выбор парсера", "Парсер");

                SelectItemViewModel selectParserViewModel = DialogDocumentManagerService.ShowView<SelectItemViewModel>(selectParserParameter, this);

                if (!selectParserViewModel.IsOk)
                {
                    return;
                }

                parserSettingsDto = contractor.ParserSettings.First(x => x.Id == selectParserViewModel!.SelectedItem!.Value.Id);
            }
            else
            {
                parserSettingsDto = parserSettings.First();
            }

            if (!OpenFileDialogService.ShowDialog())
            {
                return;
            }

            string filePath = OpenFileDialogService.GetFullFileName();

            try
            {
                ExcelImportResult<ProductSaveDto> importResult = ExcelPricesImportEngine.ImportFromXlsx(filePath, parserSettingsDto);

                if (!importResult.IsSuccess)
                {
                    MessageFacadeService.ShowValidationResultView("Ошибки при импорте файла", importResult.Errors, this);
                }
                else
                {
                    if (importResult.Errors.Any())
                    {
                        MessageFacadeService.ShowValidationResultView("Предупреждения при импорте файла", importResult.Errors, this);
                    }
                }

                ProductSaveDto[] productsToSave = importResult.ResultItems.Where(x => x.ParserCategoryId > 0).ToArray();

                IReadOnlyCollection<ValidationResultItem> resultValidationResultItems = await SaveProductsToParserServiceAsync(parserSettingsDto, productsToSave);

                if (resultValidationResultItems.Any())
                {
                    MessageFacadeService.ShowValidationResultView("Ошибки валидации.", resultValidationResultItems, this);
                }
            }
            catch (IOException exception) when (exception.Message.Contains("being used by another process"))
            {
                MessageFacadeService.ShowNotificationError($"Файл {filePath} занят другим процессом");
            }
            catch (RpcException exception)
            {
                MessageFacadeService.ShowNotificationError($"Ошибка при сохранении цен. {exception.Message}");
                Logger.LogError(exception, "Failed to save parsed prices from excel");
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to import {FileName}", filePath);
                MessageFacadeService.ShowNotificationError($"Ошибка при импорте. {exception.Message}");
            }
        }

        private async Task<IReadOnlyCollection<ValidationResultItem>> SaveProductsToParserServiceAsync(ParserSettingsDto parserSettingsDto, IReadOnlyCollection<ProductSaveDto> productsToSave)
        {
            List<ValidationResultItem> validationResultItems = new List<ValidationResultItem>();

            Result<IReadOnlyCollection<ProductSaveDto>> validateResult = ParserProductPriceValidator.ValidateAsync(productsToSave);

            if (validateResult?.Warnings?.Any() == true)
            {
                validationResultItems.AddRange(validateResult.Warnings.Select(x => new ValidationResultItem(x, false)));
            }

            if (validateResult?.IsSuccess == true)
            {
                if (validateResult.Data?.Any() != true || (validationResultItems.Any() && !MessageFacadeService.Confirm("Есть невалидные данные о товарах в файле. Продолжить сохранение только валидных товаров?")))
                {
                    return validationResultItems;
                }

                ResultMessage resultMessage = await ParserClient.SaveProductsAsync(new SaveProductsRequest { ParserSettingsId = parserSettingsDto.Id, Products = validateResult.Data });

                if (resultMessage?.ErrorObj == null)
                {
                    MessageFacadeService.ShowNotificationInfo("Файл успешно обработан");
                }
                else
                {
                    validationResultItems.AddRange(new[] { new ValidationResultItem($"Ошибки сохранения данных {resultMessage.ErrorObj?.ErrorMessage_}", true) });
                }

                return validationResultItems;
            }

            string message = !string.IsNullOrWhiteSpace(validateResult?.ErrorObj?.ErrorMessage)
                ? validateResult.ErrorObj?.ErrorMessage
                : "Не получилось спарсить ни один товар";

            validationResultItems.AddRange(new[] { new ValidationResultItem(message, true) });

            return validationResultItems;
        }

        private async Task RefreshCurrenciesAsync()
        {
            List<TransferObjects.CurrencyTypeRateDto> currencyTypeRateDtos = await WebClient.ExecuteApiRequestAsync(new QueryCurrencyTypeRates());

            _conversationRates = currencyTypeRateDtos
                .Where(x => x.ToCurrencyTypeId == CurrencyTypeIds.UahId)
                .ToDictionary(x => x.FromCurrencyTypeId, x => x.ConversionRate);
        }

        private async Task RefreshSegmentsAsync()
        {
            List<SegmentDto> segmentsList = await WebClient.ExecuteApiRequestAsync(new QuerySegments(), true);

            _segmentToolTips = segmentsList
                .Select(x => x.GetSegmentCategoryFeatures())
                .Where(x => x.Any())
                .ToDictionary(f => f.First().SegmentId, f => f.GetSegmentFeaturesString(true));
        }

        private async Task SaveAsync()
        {
            try
            {
                ProductPriceViewItem[] changed = GetChangedProducts().ToArray();

                if (changed.Length == 0)
                {
                    MessageFacadeService.ShowNotificationWarning("Нечего сохранять");
                    return;
                }

                if (!MessageFacadeService.Confirm($"Будет {GetSaveWordByNumber(changed.Length)} {changed.Length} {GetProductWordByNumber(changed.Length)}, продолжить?"))
                {
                    return;
                }

                var firstWithErrors = changed
                    .Where(x => x.GetErrors().Any())
                    .Select(x => new { Item = x, Errors = x.GetErrors().ToArray() })
                    .FirstOrDefault();

                if (firstWithErrors != null)
                {
                    CurrentProduct = firstWithErrors.Item;
                    MessageFacadeService.ShowNotificationWarning(string.Join(Environment.NewLine, firstWithErrors.Errors));
                    return;
                }

                ProductPriceSaveDto[] data = changed
                    .Select(x => new ProductPriceSaveDto(
                        x.Id,
                        x.Avail.Id,
                        x.AvailModifiedBy,
                        x.LabelRetail?.Id,
                        x.LabelWholesale?.Id,
                        x.RobotModeManual.Id,
                        x.RobotModeAuto.Id,
                        x.PriceComment,
                        x.Hotline,
                        x.Ratio,
                        x.MinLeftover,
                        x.Rozetka,
                        x.Monomarket,
                        x.FreeDelivery,
                        x.ShowInAccessories,
                        x.PlannedLeftover,
                        x.BonusTypeId,
                        x.BonusesToCharge,
                        x.MaxTradeInPrice,
                        x.Mining,
                        x.ShowcasePickupModeId,
                        x.ReservedQuantity,
                        x.GetEditablePriceDatas()
                            .Where(y => y.IsChanged)
                            .Select(y => new ProductPriceDataSaveDto(
                                y.GetPriceKind().Id,
                                y.GetPriceValue(),
                                y.GetPricePrevValue(),
                                y.MaxBonusesToUse,
                                y.GetTagColor().Id,
                                y.GetPartialPay(),
                                y.GetPartialPayPb(),
                                y.GetPartialPayPumb(),
                                y.GetPartialPayAb()))
                            .ToArray()))
                    .ToArray();

                Result<ProductPricesDto> result = await PricesClient.SavePricesAsync(new SavePricesRequest(data), this);

                if (result?.Data is null)
                {
                    return;
                }

                if (CurrentProduct != null)
                {
                    ProductInformation.ClearProduct();
                    ProductInformation.ProductId = new ProductInfoId(CurrentProduct.Id, CurrentProduct.Telemart1PriceData.DisplayCurrencyId);
                }

                Dictionary<int, ProductPriceDto> resDictionary = result.Data?.ProductPrices?.ToDictionary(x => x.Id);

                if (resDictionary != null)
                {
                    IPriceConverter priceConverter = await PriceConverterFactory.CreateAsync();

                    foreach (ProductPriceViewItem viewItem in changed)
                    {
                        ProductPriceDto product = resDictionary[viewItem.Id];

                        MapToViewItem(product, viewItem, priceConverter);
                    }

                    MessageFacadeService.ShowNotificationInfo($"{GetSaveWordByNumber(result.Data.ProductPrices.Count).Transform(To.TitleCase)} {result.Data.ProductPrices.Count} {GetProductWordByNumber(result.Data.ProductPrices.Count)}");
                }
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to save product prices");
                MessageFacadeService.ShowNotificationError("Ошибка при сохранении");
            }

            static string GetProductWordByNumber(int count)
            {
                return WordEndingHelper.GetWordByNumber(count, new[] { "товар", "товара", "товаров" });
            }

            static string GetSaveWordByNumber(int count)
            {
                return WordEndingHelper.GetWordByNumber(count, new[] { "сохранен", "сохранено", "сохранено" });
            }
        }

        private IEnumerable<ProductPriceViewItem> GetChangedProducts()
        {
            if (Products == null)
            {
                yield break;
            }

            foreach (ProductPriceViewItem item in Products.Where(x => x.IsChanged || x.GetEditablePriceDatas().Any(y => y.IsChanged)))
            {
                yield return item;
            }
        }

        private ProductPriceViewItem MapToViewItem(ProductPriceDto source, ProductPriceViewItem destination, IPriceConverter priceConverter)
        {
            destination.Id = source.Id;
            destination.Name = source.Name;
            destination.NameUkr = source.NameUkr;
            destination.NameEn = source.NameEn;
            destination.AvailModifiedBy = source.AvailModifiedBy;
            destination.Active = source.Active;
            destination.Pn = source.Pn;
            destination.ProductTypeId = source.ProductTypeId;
            destination.Preorder = source.Preorder;
            destination.CreatedOn = source.CreatedOn;
            destination.Visits = source.Visits;
            destination.InStock = source.InStock;
            destination.StorageInStock = source.StorageInStock;
            destination.TransitInStock = source.TransitInStock;
            destination.ShowcasesStock = source.ShowcasesStock;
            destination.MainStock = source.MainStock;
            destination.ShowcasesCapacity = source.ShowcasesCapacity;
            destination.DaysInStock = source.DaysInStock;
            destination.Position = source.Position;
            destination.SalesCurrentMonth = source.SalesCurrentMonth;
            destination.SalesLastMonth = source.SalesLastMonth;
            destination.SalesBeforeLastMonth = source.SalesBeforeLastMonth;
            destination.OrdersCurrentMonth = source.OrdersCurrentMonth;
            destination.OrdersLastMonth = source.OrdersLastMonth;
            destination.F2Markup = source.F2Markup;
            destination.MinProductMarginPercent = source.MinProductMarginPercent;
            destination.MinCategoryMarginPercent = source.MinCategoryMarginPercent;
            destination.PlannedMarkup = source.PlannedMarkup;
            destination.UsePlannedMarkupInAutoShowcase = source.UsePlannedMarkupInAutoShowcase;
            destination.LastSalePriceUsd = source.LastSalePriceUsd;
            destination.OrdersBeforeLastMonth = source.OrdersBeforeLastMonth;
            destination.Conversion = source.Conversion;
            destination.DaysFromLastSale = source.DaysFromLastSale;
            destination.AvailOld = Dictionaries.GetItemById<ProductAvailability>(source.AvailId);
            destination.Avail = Dictionaries.GetItemById<ProductAvailability>(source.AvailId);
            destination.LabelRetail = Dictionaries.GetItemById<ProductLabel>(source.LabelRetailId ?? 0);
            destination.LabelRetailOld = destination.LabelRetail;
            destination.LabelWholesale = Dictionaries.GetItemById<ProductLabel>(source.LabelWholesaleId ?? 0);
            destination.LabelWholesaleOld = destination.LabelWholesale;
            destination.PriceInUsd = source.PriceInUsd;
            destination.PriceCompetitor = source.PriceCompetitorUsd;
            destination.PriceTransit = source.PriceTransitUsd;
            destination.PriceCommentOld = source.PriceComment;
            destination.PriceComment = source.PriceComment;
            destination.HotlineOld = source.Hotline;
            destination.Hotline = source.Hotline;
            destination.RatioOld = source.Ratio;
            destination.Ratio = source.Ratio;
            destination.SegmentName = string.IsNullOrWhiteSpace(source.SegmentName) ? "Без сегмента" : source.SegmentName;
            destination.SegmentProfitAbcId = source.SegmentProfitAbcId;
            destination.SegmentOrderQuantityAbcId = source.SegmentOrderQuantityAbcId;
            destination.SegmentPriceOutAbcId = source.SegmentPriceOutAbcId;
            destination.ProductCategoryProfitAbcId = source.ProductCategoryProfitAbcId;
            destination.ProductCategoryOrderQuantityAbcId = source.ProductCategoryOrderQuantityAbcId;
            destination.ProductCategoryPriceOutAbcId = source.ProductCategoryPriceOutAbcId;
            destination.SegmentCategoryProfitAbcId = source.SegmentCategoryProfitAbcId;
            destination.SegmentCategoryOrderQuantityAbcId = source.SegmentCategoryOrderQuantityAbcId;
            destination.SegmentCategoryPriceOutAbcId = source.SegmentCategoryPriceOutAbcId;
            destination.HotlinePosition = source.HotlinePosition;
            destination.HotlineMinPriceUsd = source.HotlineMinPriceUsd;
            destination.HotlinePriceUsd = source.HotlinePriceUsd;
            destination.MinLeftover = source.MinLeftover;
            destination.MinLeftoverOld = source.MinLeftover;
            destination.PlannedLeftover = source.PlannedLeftover;
            destination.RozetkaOld = source.Rozetka;
            destination.Rozetka = source.Rozetka;
            destination.MonomarketOld = source.Monomarket;
            destination.Monomarket = source.Monomarket;
            destination.Mining = source.Mining;
            destination.MiningOld = source.Mining;
            destination.ShowcasePickupModeId = source.ShowcasePickupModeId;
            destination.ShowcasePickupModeIdOld = source.ShowcasePickupModeId;
            destination.ReservedQuantity = source.ReservedQuantity;
            destination.ReservedQuantityOld = source.ReservedQuantity;
            destination.Density1Month = source.Density1Month;
            destination.Density14Days = source.Density14Days;
            destination.FreeDelivery = source.FreeDelivery;
            destination.FreeDeliveryOld = source.FreeDelivery;
            destination.ShowInAccessories = source.ShowInAccessories;
            destination.ShowInAccessoriesOld = source.ShowInAccessories;
            destination.ProductCurrencyId = Currency.GetByName(source.Currency).Id;
            destination.UsdCurrency = source.UsdCurrency;
            destination.UsdConversionRate = _conversationRates[source.UsdCurrency];
            destination.RobotModeManual = GetRobotModeById(source.RobotModeManualId);
            destination.RobotModeManualOld = GetRobotModeById(source.RobotModeManualId);
            destination.RobotModeAuto = GetRobotModeById(source.RobotModeAutoId);
            destination.RobotModeAutoOld = GetRobotModeById(source.RobotModeAutoId);
            destination.CategoryId = source.CategoryId;
            destination.CategoryName = _categoryNames.GetValueOrDefault(source.CategoryId, string.Empty);
            destination.CategoryRobotModeManual = GetRobotModeById(source.CategoryRobotModeManualId);
            destination.CategoryRobotModeAuto = GetRobotModeById(source.CategoryRobotModeAutoId);

            destination.Promo = source.ProductPromo;

            destination.BonusTypeIdOld = source.BonusTypeId;
            destination.BonusesToChargeOld = source.BonusesToCharge;

            destination.BonusTypeId = source.BonusTypeId;
            destination.BonusesToCharge = source.BonusesToCharge;

            destination.WarrantyRetail = source.WarrantyRetail;
            destination.MaxTradeInPrice = source.MaxTradeInPrice;

            destination.OldMaxTradeInPrice = source.MaxTradeInPrice;

            if (_segmentToolTips.TryGetValue(source.SegmentId, out string segmentToolTip))
            {
                destination.SegmentToolTip = segmentToolTip;
            }

            destination.SupplierPrices = source.SupplierPrices?
                .Select(x => new ContractorPrice(x.ContractorId, x.ContractorName, x.AllowDocuments, x.AbcId, x.PriceUsd, x.CreatedOn))
                .ToArray() ?? Array.Empty<ContractorPrice>();

            destination.CompetitorPrices = source.CompetitorPrices?
                .Select(x => new ContractorPrice(x.ContractorId, x.ContractorName, x.AllowDocuments, x.AbcId, x.PriceUsd, x.CreatedOn))
                .ToArray() ?? Array.Empty<ContractorPrice>();

            destination.RrpPrices = source.RrpPrices?
                .Select(x => new ContractorPrice(x.ContractorId, x.ContractorName, x.AllowDocuments, x.AbcId, x.PriceUsd, x.CreatedOn))
                .ToArray() ?? Array.Empty<ContractorPrice>();

            destination.ConfiguratorPrices = source.ConfiguratorPrices?
                .Select(x => new ContractorPrice(x.ContractorId, x.ContractorName, x.AllowDocuments, x.AbcId, x.PriceUsd, x.CreatedOn))
                .ToArray() ?? Array.Empty<ContractorPrice>();

            destination.HotlineMinCompetitorClassPrices = source.HotlineAbcClassPrices?
                .Select(x => new ContractorPrice(x.AbcId, x.AbcName, false, x.AbcId, x.PriceUsd, default))
                .ToArray() ?? Array.Empty<ContractorPrice>();

            destination.OrderSales = source.Sales?.Select(x => x.OrdersCount).ToArray() ?? Array.Empty<int>();
            destination.ProductSales = source.Sales?.Select(x => x.ProductsCount).ToArray() ?? Array.Empty<int>();

            destination.SalesAvgPrices = source.Sales?.Select(x => x.AvgPriceUsd).ToArray() ?? Array.Empty<double>();
            destination.PurchasesAvgPrices = source.Purchases?.Select(x => x.AvgPriceUsd).ToArray() ?? Array.Empty<double>();
            destination.PromoWeights = source.PromoHistory?.Select(x => x.Weigth).ToArray() ?? Array.Empty<double>();
            destination.PromoHistory = source.PromoHistory;
            destination.Purchases = source.Purchases;
            destination.HotlineAbcClassPrices = source.HotlineAbcClassPrices;
            destination.Sales = source.Sales;

            destination.SearchTemplateProducts = source.SearchTemplatePrices
                ?.Select(x => new SearchTemplateProduct(x.SearchTemplateId, x.SearchTemplateName, x.Price, x.ContractorName, x.ContractorId))
                .ToArray() ?? ArraySegment<SearchTemplateProduct>.Empty;

            int? displayCurrencyId = GetDisplayCurrency(DisplayCurrencyMode, destination.ProductCurrencyId);

            ProductPriceKind telemart1 = Dictionaries.GetItemById<ProductPriceKind>(ProductPriceKind.Telemart1);
            ProductPriceKind telemart2 = Dictionaries.GetItemById<ProductPriceKind>(ProductPriceKind.Telemart2);
            ProductPriceKind telemart3 = Dictionaries.GetItemById<ProductPriceKind>(ProductPriceKind.Telemart3);
            ProductPriceKind telemart4 = Dictionaries.GetItemById<ProductPriceKind>(ProductPriceKind.Telemart4);
            ProductPriceKind telemart5 = Dictionaries.GetItemById<ProductPriceKind>(ProductPriceKind.Telemart5);

            ProductPriceKind configurator1 = Dictionaries.GetItemById<ProductPriceKind>(ProductPriceKind.Configurator1);
            ProductPriceKind configurator2 = Dictionaries.GetItemById<ProductPriceKind>(ProductPriceKind.Configurator2);
            ProductPriceKind configurator3 = Dictionaries.GetItemById<ProductPriceKind>(ProductPriceKind.Configurator3);
            ProductPriceKind configurator4 = Dictionaries.GetItemById<ProductPriceKind>(ProductPriceKind.Configurator4);
            ProductPriceKind configurator5 = Dictionaries.GetItemById<ProductPriceKind>(ProductPriceKind.Configurator5);
            ProductPriceKind configurator6 = Dictionaries.GetItemById<ProductPriceKind>(ProductPriceKind.Configurator6);
            ProductPriceKind configurator7 = Dictionaries.GetItemById<ProductPriceKind>(ProductPriceKind.Configurator7);
            ProductPriceKind configurator8 = Dictionaries.GetItemById<ProductPriceKind>(ProductPriceKind.Configurator8);
            ProductPriceKind configurator9 = Dictionaries.GetItemById<ProductPriceKind>(ProductPriceKind.Configurator9);
            ProductPriceKind configurator10 = Dictionaries.GetItemById<ProductPriceKind>(ProductPriceKind.Configurator10);

            if (source.BasedOnPrices?.Any() == true)
            {
                destination.ParentPrice1 = GetPriceData(source.BasedOnPrices, telemart1, displayCurrencyId, source.F2Markup, source.ContractorAllowDocument);
                destination.ParentPrice2 = GetPriceData(source.BasedOnPrices, telemart2, displayCurrencyId, source.F2Markup, source.ContractorAllowDocument);
                destination.ParentPrice3 = GetPriceData(source.BasedOnPrices, telemart3, displayCurrencyId, source.F2Markup, source.ContractorAllowDocument);
                destination.ParentPrice4 = GetPriceData(source.BasedOnPrices, telemart4, displayCurrencyId, source.F2Markup, source.ContractorAllowDocument);
                destination.ParentPrice5 = GetPriceData(source.BasedOnPrices, telemart5, displayCurrencyId, source.F2Markup, source.ContractorAllowDocument);
            }

            if (source.AssembledComputerRuleBasePrices?.Any() == true)
            {
                destination.AssembledComputerRuleBasePrice1 = GetPriceData(source.AssembledComputerRuleBasePrices, telemart1, displayCurrencyId, source.F2Markup, source.ContractorAllowDocument);
                destination.AssembledComputerRuleBasePrice2 = GetPriceData(source.AssembledComputerRuleBasePrices, telemart2, displayCurrencyId, source.F2Markup, source.ContractorAllowDocument);
                destination.AssembledComputerRuleBasePrice3 = GetPriceData(source.AssembledComputerRuleBasePrices, telemart3, displayCurrencyId, source.F2Markup, source.ContractorAllowDocument);
                destination.AssembledComputerRuleBasePrice4 = GetPriceData(source.AssembledComputerRuleBasePrices, telemart4, displayCurrencyId, source.F2Markup, source.ContractorAllowDocument);
                destination.AssembledComputerRuleBasePrice5 = GetPriceData(source.AssembledComputerRuleBasePrices, telemart5, displayCurrencyId, source.F2Markup, source.ContractorAllowDocument);
            }

            destination.Telemart1PriceData = GetPriceData(source.Prices, telemart1, displayCurrencyId, source.F2Markup, source.ContractorAllowDocument);
            destination.Telemart2PriceData = GetPriceData(source.Prices, telemart2, displayCurrencyId, source.F2Markup, source.ContractorAllowDocument);
            destination.Telemart3PriceData = GetPriceData(source.Prices, telemart3, displayCurrencyId, source.F2Markup, source.ContractorAllowDocument);
            destination.Telemart4PriceData = GetPriceData(source.Prices, telemart4, displayCurrencyId, source.F2Markup, source.ContractorAllowDocument);
            destination.Telemart5PriceData = GetPriceData(source.Prices, telemart5, displayCurrencyId, source.F2Markup, source.ContractorAllowDocument);

            destination.Configurator1PriceData = GetPriceData(source.Prices, configurator1, displayCurrencyId, source.F2Markup, source.ContractorAllowDocument);
            destination.Configurator2PriceData = GetPriceData(source.Prices, configurator2, displayCurrencyId, source.F2Markup, source.ContractorAllowDocument);
            destination.Configurator3PriceData = GetPriceData(source.Prices, configurator3, displayCurrencyId, source.F2Markup, source.ContractorAllowDocument);
            destination.Configurator4PriceData = GetPriceData(source.Prices, configurator4, displayCurrencyId, source.F2Markup, source.ContractorAllowDocument);
            destination.Configurator5PriceData = GetPriceData(source.Prices, configurator5, displayCurrencyId, source.F2Markup, source.ContractorAllowDocument);
            destination.Configurator6PriceData = GetPriceData(source.Prices, configurator6, displayCurrencyId, source.F2Markup, source.ContractorAllowDocument);
            destination.Configurator7PriceData = GetPriceData(source.Prices, configurator7, displayCurrencyId, source.F2Markup, source.ContractorAllowDocument);
            destination.Configurator8PriceData = GetPriceData(source.Prices, configurator8, displayCurrencyId, source.F2Markup, source.ContractorAllowDocument);
            destination.Configurator9PriceData = GetPriceData(source.Prices, configurator9, displayCurrencyId, source.F2Markup, source.ContractorAllowDocument);
            destination.Configurator10PriceData = GetPriceData(source.Prices, configurator10, displayCurrencyId, source.F2Markup, source.ContractorAllowDocument);

            destination.WarehousePriceData = new ProductPriceDataViewItem(
                priceConverter,
                source.UsdCurrency,
                source.PriceWarehouseUsd ?? 0,
                null,
                Currency.Usd.Id,
                null,
                displayCurrencyId,
                0,
                null,
                null,
                null,
                null,
                destination,
                TagColor.White,
                null,
                source.F2Markup,
                source.ContractorAllowDocument);

            destination.PriceInPriceData = new ProductPriceDataViewItem(
                priceConverter,
                source.UsdCurrency,
                source.PriceInUsd ?? 0,
                null,
                Currency.Usd.Id,
                null,
                displayCurrencyId,
                0,
                null,
                null,
                null,
                null,
                destination,
                TagColor.White,
                null,
                source.F2Markup,
                source.ContractorAllowDocument);

            return destination;

            ProductPriceDataViewItem GetPriceData(IReadOnlyCollection<ProductPriceSimpleDto> prices, ProductPriceKind priceKind, int? displayCurrency, double f2Markup, bool? contractorAllowDocument)
            {
                ProductPriceSimpleDto productPrice = prices?.FirstOrDefault(x => x.PriceTypeId == priceKind.Id);

                int tagColorId = productPrice?.TagColorId ?? TagColor.WhiteId;
                TagColor tagColor = Dictionaries.GetItemById<TagColor>(tagColorId) ?? TagColor.White;

                return new ProductPriceDataViewItem(
                    priceConverter,
                    source.UsdCurrency,
                    productPrice?.Price ?? 0,
                    productPrice?.PricePrev,
                    productPrice?.CurrencyId ?? Currency.UahId,
                    priceKind,
                    displayCurrency,
                    productPrice?.MaxBonusesToUse ?? 0,
                    productPrice?.PartialPay,
                    productPrice?.PartialPayPb,
                    productPrice?.PartialPayPumb,
                    productPrice?.PartialPayAb,
                    destination,
                    tagColor,
                    productPrice?.ModifiedOn.ToString(CultureInfo.InvariantCulture),
                    f2Markup,
                    contractorAllowDocument);
            }
        }

        private void MapFetchedDataToViewItem(ProductPriceDto source, ProductPriceViewItem destination, IPriceConverter priceConverter)
        {
            destination.Density1Month = source.Density1Month;
            destination.Density14Days = source.Density14Days;
            destination.Promo = source.ProductPromo;
            destination.MinProductMarginPercent = source.MinProductMarginPercent;
            destination.MinCategoryMarginPercent = source.MinCategoryMarginPercent;

            destination.SupplierPrices = source.SupplierPrices?
                .Select(x => new ContractorPrice(x.ContractorId, x.ContractorName, x.AllowDocuments, x.AbcId, x.PriceUsd, x.CreatedOn))
                .ToArray() ?? Array.Empty<ContractorPrice>();

            destination.CompetitorPrices = source.CompetitorPrices?
                .Select(x => new ContractorPrice(x.ContractorId, x.ContractorName, x.AllowDocuments, x.AbcId, x.PriceUsd, x.CreatedOn))
                .ToArray() ?? Array.Empty<ContractorPrice>();

            destination.RrpPrices = source.RrpPrices?
                .Select(x => new ContractorPrice(x.ContractorId, x.ContractorName, x.AllowDocuments, x.AbcId, x.PriceUsd, x.CreatedOn))
                .ToArray() ?? Array.Empty<ContractorPrice>();

            destination.ConfiguratorPrices = source.ConfiguratorPrices?
                .Select(x => new ContractorPrice(x.ContractorId, x.ContractorName, x.AllowDocuments, x.AbcId, x.PriceUsd, x.CreatedOn))
                .ToArray() ?? Array.Empty<ContractorPrice>();

            destination.HotlineMinCompetitorClassPrices = source.HotlineAbcClassPrices?
                .Select(x => new ContractorPrice(x.AbcId, x.AbcName, false, x.AbcId, x.PriceUsd, default))
                .ToArray() ?? Array.Empty<ContractorPrice>();

            destination.OrderSales = source.Sales?.Select(x => x.OrdersCount).ToArray() ?? Array.Empty<int>();
            destination.ProductSales = source.Sales?.Select(x => x.ProductsCount).ToArray() ?? Array.Empty<int>();
            destination.SalesAvgPrices = source.Sales?.Select(x => x.AvgPriceUsd).ToArray() ?? Array.Empty<double>();
            destination.PurchasesAvgPrices = source.Purchases?.Select(x => x.AvgPriceUsd).ToArray() ?? Array.Empty<double>();
            destination.PromoWeights = source.PromoHistory?.Select(x => x.Weigth).ToArray() ?? Array.Empty<double>();
            destination.PromoHistory = source.PromoHistory;
            destination.Purchases = source.Purchases;
            destination.HotlineAbcClassPrices = source.HotlineAbcClassPrices;
            destination.Sales = source.Sales;

            destination.SearchTemplateProducts = source.SearchTemplatePrices
                ?.Select(x => new SearchTemplateProduct(x.SearchTemplateId, x.SearchTemplateName, x.Price, x.ContractorName, x.ContractorId))
                .ToArray() ?? ArraySegment<SearchTemplateProduct>.Empty;
        }

        private void CurrentProductChangedCallback()
        {
            ProductInformation.ClearProduct();

            if (CurrentProduct != null)
            {
                ProductInformation.ProductId = new ProductInfoId(CurrentProduct.Id, CurrentProduct.Telemart1PriceData.DisplayCurrencyId);
                _robotViewModel.RefreshAsync();
            }
        }

        private void SetDisplayCurrencyMode(DisplayCurrencyMode mode)
        {
            Action<ProductPriceViewItem> setCurrency;

            switch (mode)
            {
                case DisplayCurrencyMode.Default:
                    setCurrency = x => x.SetDisplayCurrency();
                    break;
                case DisplayCurrencyMode.Product:
                    setCurrency = x => x.SetDisplayCurrency(x.ProductCurrencyId);
                    break;
                case DisplayCurrencyMode.Uah:
                    setCurrency = x => x.SetDisplayCurrency(Currency.Uah.Id);
                    break;
                case DisplayCurrencyMode.Usd:
                    setCurrency = x => x.SetDisplayCurrency(Currency.Usd.Id);
                    break;
                case DisplayCurrencyMode.Eur:
                    setCurrency = x => x.SetDisplayCurrency(Currency.Eur.Id);
                    break;
                default:
                    throw new NotSupportedException();
            }

            Products.ForEach(x => setCurrency(x));

            DisplayCurrencyMode = mode;

            if (CurrentProduct != null
                && ProductInformation.SelectedCurrencyMode == CurrencyMode.Auto
                && ProductInformation.SelectedCurrency.Id != CurrentProduct.Telemart1PriceData.DisplayCurrencyId)
            {
                ProductInformation.SetDisplayCurrency(Currency.GetById(CurrentProduct.Telemart1PriceData.DisplayCurrencyId));
            }
        }

        private void ShowCalcPricesDialog()
        {
            NotModalSizeableDocumentManagerService.ShowView("ProductPricesRobotView", _robotViewModel, null, this);
        }

        private async Task OldCalcCurrentProductPricesAsync()
        {
            if (CurrentProduct == null)
            {
                return;
            }

            (string Script, string Parameters) productRobotScript = await GetRobotScriptByCategoryAsync(CurrentProduct.CategoryId);

            if (string.IsNullOrWhiteSpace(productRobotScript.Script))
            {
                MessageFacadeService.ShowNotificationWarning("Правила робота не заполнены");
                return;
            }

            try
            {
                CalculatePriceContext context = CurrentProduct.GetContext(CurrentProduct.RobotModeManual, CreditOffers, MinPartialPayCount);

                CalculatePriceResult result = await RobotCalculator.CalculateAsync(context, RobotHelper.GetFullScript(productRobotScript.Script, productRobotScript.Parameters));

                if (result.Success)
                {
                    IPriceConverter priceConverter = await PriceConverterFactory.CreateAsync();

                    CurrentProduct.SetCalculatePriceResult(Dictionaries, result.Data, priceConverter);
                    MessageFacadeService.ShowNotificationInfo(Message(1));
                }
                else
                {
                    ShowValidationResultView("Ошибки при обработке скрипта", result.Errors);
                }
            }
            catch (PriceCalculationCompilationException exception)
            {
                MessageFacadeService.ShowMessageBox(exception.Message, "Ошибки при обработке скрипта", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to calculate price");

                MessageFacadeService.ShowMessageBox(exception.Message, "Ошибки при выполнении скрипта", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private Task OldCalcPricesAsync()
        {
            return OldCalculatePricesAsync(DialogDocumentManagerService, true, true, null, null);
        }

        private async Task CalcPricesAsync(bool onlyCurrentRow)
        {
            ProductPriceViewItem[] productViewItems;

            if (onlyCurrentRow)
            {
                if (CurrentProduct is null)
                {
                    MessageFacadeService.ShowNotificationError("Строка не выбрана");

                    return;
                }

                productViewItems = new[]
                {
                    CurrentProduct
                };
            }
            else
            {
                productViewItems = Products.ToArray();
            }

            SplashScreenManager splashScreenManager = SplashScreenManager.CreateWaitIndicator();
            splashScreenManager.ViewModel.Status = "Рассчет робота...";
            splashScreenManager.Show();

            Result fetchResult = await FetchFullDataAsync(productViewItems);

            if (!fetchResult.IsSuccess)
            {
                return;
            }

            ProductPriceDto[] products = productViewItems.Select(MapViewItemToDto).ToArray();

            CalculatePricesRequest request = new CalculatePricesRequest(true, products);

            Result<IReadOnlyCollection<ProductPriceSaveDto>> result = await PricesClient.CalculatePricesAsync(request, this);

            IPriceConverter priceConverter = await PriceConverterFactory.CreateAsync();

            foreach (ProductPriceSaveDto saveDto in result.Data)
            {
                ProductPriceViewItem product = Products.First(x => x.Id == saveDto.Id);

                product.SetCalculatePriceResult(Dictionaries, saveDto, priceConverter.Copy());
            }

            splashScreenManager.Close();
        }

        private async Task ShowRrpPriceViolatorsAsync()
        {
            await FetchFullDataAsync(Products);

            IPriceConverter priceConverter = await PriceConverterFactory.CreateAsync();

            IReadOnlyCollection<ComboBoxItem> rrpSuppliers = Products
                .SelectMany(x => x.RrpPrices)
                .Select(x => new ComboBoxItem(x.ContractorId, x.ContractorName))
                .Distinct()
                .OrderBy(x => x.DisplayValue)
                .ToReadOnlyObservableCollection();

            IReadOnlyCollection<ComboBoxItem> competitors = Products
                .SelectMany(x => x.CompetitorPrices)
                .Select(x => new ComboBoxItem(x.ContractorId, x.ContractorName))
                .Distinct()
                .OrderBy(x => x.DisplayValue)
                .ToReadOnlyObservableCollection();

            if (!rrpSuppliers.Any())
            {
                MessageFacadeService.ShowNotificationWarning("Нет товаров с РРЦ");
            }
            else if (!competitors.Any())
            {
                MessageFacadeService.ShowNotificationWarning("Нет товаров с ценами конкурентов");
            }
            else
            {
                IReadOnlyCollection<ViolatorsRrpProduct> productPrices = Products
                    .Select(x => new ViolatorsRrpProduct(
                        x.Id,
                        x.Name,
                        x.RrpPrices
                            .Select(y => new ViolatorsRrpContractorPrice(y.ContractorId, priceConverter.Convert(y.PriceUsd, Currency.UsdId, Currency.UahId, x.UsdCurrency)))
                            .ToArray(),
                        x.CompetitorPrices
                            .Select(y => new ViolatorsRrpContractorPrice(y.ContractorId, priceConverter.Convert(y.PriceUsd, Currency.UsdId, Currency.UahId, x.UsdCurrency)))
                            .ToArray()))
                    .ToArray();

                DialogDocumentManagerService.ShowView<ViolatorsRrpPriceViewModel>(new ViolatorsRrpPriceParameter(rrpSuppliers, competitors, productPrices), this);
            }
        }

        private PriceRobotMode GetRobotModeById(int id)
        {
            return Dictionaries.GetItemById<PriceRobotMode>(id);
        }

        private async Task SearchByFilterAsync()
        {
            FilterItems = null;

            if (SelectedCategory == null)
            {
                return;
            }

            try
            {
                IEnumerable<RootAccordionItem> filterItems = await FiltersLoader.QueryCategoryFiltersAsync(
                    SelectedCategory.Id,
                    Constants.TelemartContractorId,
                    false);

                FilterItems = filterItems.ToObservableCollection();
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to load filters");
                MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
            }
        }

        private void ShowValidationResultView(string title, IReadOnlyCollection<string> errors)
        {
            SizeableDialogDocumentManagerService.ShowView<ValidationResultViewModel>(
                new ValidationResultViewModelParameter(title, errors.Select(x => new ValidationResultItem(x, true)).ToArray()),
                this);
        }

        private void SelectedCategoryChanged()
        {
            if (DocumentManagerService.ActiveDocument.Title is ModuleHeader moduleHeader)
            {
                string title = SelectedCategory != null
                    ? $"Цены ({SelectedCategory.Name})"
                    : "Цены";

                moduleHeader.SetTitle(title);
            }
        }

        private void HandleTableViewLoaded(RoutedEventArgs args)
        {
            TableView = args.Source as TableView;
        }

        private void ShowPropertyChanges()
        {
            ProductPricePropertyChangesParameter parameter = new ProductPricePropertyChangesParameter(CurrentProduct.PropertyChanges, CurrentProduct.Name);

            DialogDocumentManagerService.ShowView<ProductPricePropertyChangesViewModel>(parameter, this);
        }

        private void Export(TableView tableView)
        {
            if (tableView?.Grid == null)
            {
                return;
            }

            string selectedCategoryName = SelectedCategory == null ? string.Empty : $"_{SelectedCategory.Name}";
            string fileName = $"Product_Prices{selectedCategoryName}_{DateTime.Now:yyyy-MM-dd}";
            string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            SaveFileDialogService.ShowDialog(
                _ =>
                {
                    List<ColumnBase> columnChooserColumns = new List<ColumnBase>(tableView.ColumnChooserColumns);

                    foreach (ColumnBase tableViewColumnChooserColumn in columnChooserColumns)
                    {
                        tableViewColumnChooserColumn.Visible = true;
                    }

                    string filePath = SaveFileDialogService.File.GetFullName();

                    tableView.ExportToXlsx(filePath, new XlsxExportOptionsEx(TextExportMode.Value));

                    MessageFacadeService.ShowNotificationInfo("Данные успешно сохранены");

                    foreach (ColumnBase tableViewColumnChooserColumn in columnChooserColumns)
                    {
                        tableViewColumnChooserColumn.Visible = false;
                    }
                },
                folderPath,
                fileName);
        }

        private void CancelFilter()
        {
            SearchName = null;
            SelectedContractors = null;
            SelectedCategory = null;
            SelectedAvailTypes = null;
        }

        private void Robot()
        {
            int categoryId;
            string categoryName;

            if (CurrentProduct is not null)
            {
                categoryId = CurrentProduct.CategoryId;
                categoryName = CurrentProduct.CategoryName;
                MessageFacadeService.ShowNotificationInfo("Настройки взяты из бренда товара");
            }
            else
            {
                categoryId = SelectedCategory.Id;
                categoryName = SelectedCategory.Name;
                MessageFacadeService.ShowNotificationInfo("Настройки взяты из родительской категории");
            }

            RobotCategoryParameter parameter = new RobotCategoryParameter(categoryId, categoryName);

            NonModalSizeableDialogDocumentManagerService.ShowView<RobotCategoryPropertyViewModel>(parameter, this);
        }

        private async Task<Result> FetchFullDataAsync(IReadOnlyCollection<ProductPriceViewItem> productPriceViewItems)
        {
            int[] productIdsToFetch = productPriceViewItems.Where(x => !x.ReadyForPriceCalculation).Select(x => x.Id).ToArray();

            if (!productIdsToFetch.Any())
            {
                return Result.Success();
            }

            Result<ProductPricesDto> queryProductPricesResult = await QueryProductPricesAsync(productIdsToFetch, SelectedContractors?.Select(x => x.Id).ToArray());

            if (!queryProductPricesResult.IsSuccess)
            {
                const string ruErrorMessage = "Не удалось загрузить данные";

                MessageFacadeService.ShowNotificationError(ruErrorMessage);
                return Result.Error("Failed to fetch calculation data", ruErrorMessage);
            }

            foreach (ProductPriceDto dto in queryProductPricesResult.Data.ProductPrices)
            {
                ProductPriceViewItem viewItem = productPriceViewItems.First(x => x.Id == dto.Id);

                MapFetchedDataToViewItem(dto, viewItem, _priceConverter);

                viewItem.ReadyForPriceCalculation = true;
            }

            return Result.Success();
        }
    }
}