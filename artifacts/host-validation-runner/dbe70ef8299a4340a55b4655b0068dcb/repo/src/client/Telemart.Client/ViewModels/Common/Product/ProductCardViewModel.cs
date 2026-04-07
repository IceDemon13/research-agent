using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Core;
using DevExpress.XtraPrinting.Native;
using MediatR;
using Microsoft.Extensions.Logging;
using Telemart.Client.Business;
using Telemart.Client.Business.Parser;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.ReportFactory;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Settings.Printing;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Core.Helpers;
using Telemart.Client.Data.Requests.Features.Category;
using Telemart.Client.Data.Requests.Features.Content;
using Telemart.Client.Data.Requests.Features.Contractor;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.Requests.Features.Parser;
using Telemart.Client.Data.Requests.Features.Parser.Actions;
using Telemart.Client.Data.Requests.Features.Products;
using Telemart.Client.Data.Requests.Features.Products.Actions;
using Telemart.Client.Data.Requests.Features.Setting;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Mediator.Requests;
using Telemart.Client.Properties;
using Telemart.Client.Reports.AssemblyService;
using Telemart.Client.Reports.Common;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Content;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Common.PrintBarcodeParameters;
using Telemart.Client.ViewModels.Common.PrintImportStickerParameters;
using Telemart.Client.ViewModels.Dialogs;
using Telemart.Client.ViewModels.Directories.ProductsCatalog;
using Telemart.Client.ViewModels.Parser.Dictionary;
using Telemart.Client.ViewModels.Store.Order;
using Telemart.Client.ViewModels.Store.Order.ProductInformation;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Common.Product
{
    internal sealed class ProductCardViewModel : TelemartDialogViewModelBase, ISupportHotkeys
    {
        private const string Separator = ", ";
        private readonly int[] productDayCategoryIds = { 5, 189, 197, 204, 396, 421, 1107, 1866, 1869 };

        private int productId;
        private Dictionary<int, string> contractorsDictionary;
        private Dictionary<int, string> categoriesDictionary;

        public ProductCardViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            IMessenger messenger,
            IMediator mediator,
            IBarcodeReportFactory barcodeReportFactory,
            IErrorHandler errorHandler,
            ProductInformationViewModel productInformationViewModel,
            IPrintingSettingsStore printingSettings)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Mapper = mapper;
            Messenger = messenger;
            Mediator = mediator;
            BarcodeReportFactory = barcodeReportFactory;
            ErrorHandler = errorHandler;
            PrintingSettings = printingSettings;

            RemoveBarcodeCommand = new AsyncCommand(RemoveBarcodeAsync, () => SelectedBarcode != null && WebClient.IsOperationAllowed(BusinessOperation.ProductDeleteBarcode));
            PrintOurBarcodeCommand = new AsyncCommand(PrintBarcodeAsync);
            PrintImportStickerCommand = new AsyncCommand(PrintImportStickerAsync);
            HandleSelectionChangedCommand = new DelegateCommand<ValueChangedEventArgs<FrameworkElement>>(HandleSelectionChanged);
            RefreshParserAliasesCommand = new AsyncCommand(RefreshParserAliasesAsync);
            UnchainParserAliasCommand = new AsyncCommand<ParserAliasViewItemWrapper>(UnchainParserAliasAsync, x => x != null);
            RemoveParserAliasCommand = new AsyncCommand<ParserAliasViewItemWrapper>(RemoveParserAliasAsync, x => x != null);
            CreateBarcodeCommand = new DelegateCommand(CreateBarcode);
            AddSerialNumberLengthCommand = new DelegateCommand(AddSerialNumberLength, () => webClient.IsOperationAllowed(BusinessOperation.ProductEditSerialNumberLength) && ProductCard?.KeepSerial == true);
            RemoveSerialNumberLengthCommand = new DelegateCommand(RemoveSerialNumberLength, () => SelectedSerialNumberLength != null && webClient.IsOperationAllowed(BusinessOperation.ProductEditSerialNumberLength) && ProductCard?.KeepSerial == true);
            MoveCommand = new DelegateCommand(Move);
            AddSerialNumberLengthByScanCommand = new DelegateCommand(AddSerialNumberLengthByScan, () => webClient.IsOperationAllowed(BusinessOperation.ProductEditSerialNumberLength) && ProductCard?.KeepSerial == true);
            ClearCashCommand = new AsyncCommand(ClearCashAsync);

            Messenger.Register<ProductBarcodeMessage>(this, OnProductBarcodeMessage);

            ProductInformation = productInformationViewModel;
        }

        public ProductCardViewModel()
        {
        }

        #region Commands

        public IDelegateCommand HandleSelectionChangedCommand { get; }

        public IAsyncCommand PrintOurBarcodeCommand { get; }

        public IAsyncCommand PrintImportStickerCommand { get; }

        public IAsyncCommand RemoveBarcodeCommand { get; }

        public IDelegateCommand RemoveSerialNumberLengthCommand { get; }

        public IAsyncCommand RefreshParserAliasesCommand { get; }

        public IAsyncCommand UnchainParserAliasCommand { get; }

        public IAsyncCommand RemoveParserAliasCommand { get; }

        public IDelegateCommand CreateBarcodeCommand { get; }

        public IDelegateCommand AddSerialNumberLengthCommand { get; }

        public IDelegateCommand MoveCommand { get; }

        public IDelegateCommand AddSerialNumberLengthByScanCommand { get; }

        public IAsyncCommand ClearCashCommand { get; }

        #endregion

        #region INPC

        public ReadOnlyObservableCollection<ComboBoxItem> Employees
        {
            get { return GetProperty(() => Employees); }
            private set { SetProperty(() => Employees, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> ProductDayCategories
        {
            get { return GetProperty(() => ProductDayCategories); }
            private set { SetProperty(() => ProductDayCategories, value); }
        }

        public bool IsHelpVisible
        {
            get { return GetProperty(() => IsHelpVisible); }
            set { SetProperty(() => IsHelpVisible, value); }
        }

        public ProductCardViewItem ProductCard
        {
            get { return GetProperty(() => ProductCard); }
            private set { SetProperty(() => ProductCard, value); }
        }

        public ProductBarcodeViewItem SelectedBarcode
        {
            get { return GetProperty(() => SelectedBarcode); }
            set { SetProperty(() => SelectedBarcode, value); }
        }

        public ProductSnLengthViewItem SelectedSerialNumberLength
        {
            get { return GetProperty(() => SelectedSerialNumberLength); }
            set { SetProperty(() => SelectedSerialNumberLength, value); }
        }

        public ObservableCollection<ParserAliasViewItemWrapper> ParserAliases
        {
            get { return GetProperty(() => ParserAliases); }
            set { SetProperty(() => ParserAliases, value); }
        }

        public ParserAliasViewItemWrapper SelectedParserAlias
        {
            get { return GetProperty(() => SelectedParserAlias); }
            set { SetProperty(() => SelectedParserAlias, value); }
        }

        public List<ActiveValue> ActiveValues
        {
            get { return GetProperty(() => ActiveValues); }
            set { SetProperty(() => ActiveValues, value); }
        }

        public ActiveValue SelectedActiveValue
        {
            get { return GetProperty(() => SelectedActiveValue); }
            set { SetProperty(() => SelectedActiveValue, value, () => ProductCard.Active = SelectedActiveValue.Id); }
        }

        public ReadOnlyObservableCollection<ProductType> Types
        {
            get { return GetProperty(() => Types); }
            set { SetProperty(() => Types, value); }
        }

        public ProductInformationViewModel ProductInformation
        {
            get { return GetProperty(() => ProductInformation); }
            private set { SetProperty(() => ProductInformation, value); }
        }

        public bool AllowBarcodeActive => WebClient.IsOperationAllowed(BusinessOperation.ProductDeactiveBarcode);

        #endregion

        #region DialogSettings

        public override int Height => 400;

        public override int MinHeight => 400;

        public override int MinWidth => 510;

        public override int Width => 510;

        #endregion

        public bool IsAdmin => WebClient.AuthenticatedEmployee.HasAnyRole(Role.Admin);

        public bool IsProduct => WebClient.AuthenticatedEmployee.HasAnyRole(Role.Product);

        public bool IsContent => WebClient.AuthenticatedEmployee.HasAnyRole(Role.Content);

        public bool IsMarketer => WebClient.AuthenticatedEmployee.HasAnyRole(Role.Marketer);

        public bool IsWarehouse => WebClient.AuthenticatedEmployee.HasAnyRole(Role.Warehouse);

        public bool IsVisibleClearCash => WebClient.IsOperationAllowed(BusinessOperation.ProductCleanCash);

        public bool CanEditKeepSerial => WebClient.IsOperationAllowed(BusinessOperation.ProductEditKeepSerial);

        public bool IsAdminOrWarehouse => IsAdmin || IsWarehouse;

        public bool IsAdminOrProduct => IsAdmin || IsProduct;

        public bool CanMove
        {
            get { return GetProperty(() => CanMove); }
            private set { SetProperty(() => CanMove, value); }
        }

        private IMapper Mapper { get; }

        private IMediator Mediator { get; }

        private IMessenger Messenger { get; }

        private IBarcodeReportFactory BarcodeReportFactory { get; }

        private IErrorHandler ErrorHandler { get; }

        private IPrintingSettingsStore PrintingSettings { get; }

        public bool HandleHotkey(HotkeyMessage hotkeyMessage)
        {
            bool handled = false;

            switch (hotkeyMessage.Key)
            {
                case Key.P:
                    {
                        if (hotkeyMessage.ModifierKeys == ModifierKeys.Control)
                        {
                            PrintOurBarcodeCommand.Execute(null);
                            handled = true;
                        }

                        break;
                    }

                case Key.Delete:
                    {
                        if (RemoveBarcodeCommand.CanExecute(null))
                        {
                            RemoveBarcodeCommand.Execute(null);
                            handled = true;
                        }

                        break;
                    }
            }

            return handled;
        }

        protected override async Task HandleLoadedAsync()
        {
            CanMove = WebClient.IsOperationAllowed(BusinessOperation.ProductMove);

            ActiveValues = ActiveValue.GetProductCardAvailableValues().ToList();
            Types = Dictionaries.GetItems<ProductType>().Where(x => !x.IsVirtual).ToReadOnlyObservableCollection();

            await Task.WhenAll(RefreshEmployeesAsync(), RefreshCategoriesAsync());

            ProductCardDto dto = await WebClient.ExecuteApiRequestAsync(new QueryProductCard(productId));

            SetData(dto);

            Title = $"Товар {ProductCard.Name} ({ProductCard.ProductId.ToString(CultureInfo.InvariantCulture)})";
        }

        protected override async Task HandleOkAsync()
        {
            ProductCardSaveDto saveDto = Mapper.Map<ProductCardSaveDto>(ProductCard);

            try
            {
                Result<ProductCardDto> result = await WebClient.ExecuteApiRequestAsync(new UpdateProductCard(saveDto));

                if (result.Warnings.Any())
                {
                    MessageFacadeService.ShowNotificationWarning("Настройки товара сохранены c предупреждениями");
                    ShowValidationResultView("Предупреждения", result.Warnings.Select(x => new ValidationResultItem(x, false)).ToList());
                }
                else
                {
                    MessageFacadeService.ShowNotificationInfo("Настройки товара успешно сохранены");
                }

                Messenger.Send(new ProductCardMessage(result.Data, MessageType.Changed));
                Close();
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при сохранении настроек товара");
                ShowValidationResultView("Ошибки при сохранении настроек товара", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to update product card");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Error while updating product card");
                MessageFacadeService.ShowNotificationError("Ошибка при сохранении настроек товара");
            }
        }

        protected override void OnParameterChanged(object parameter)
        {
            if (IsInDesignMode)
            {
                productId = 1;
                IsHelpVisible = true;
                return;
            }

            productId = ((ProductCardViewMessage)parameter).ProductId;
        }

        protected override void OnInitializeInDesignMode()
        {
            IsHelpVisible = true;

            base.OnInitializeInDesignMode();
        }

        private static ParserAliasViewItem MapTransferObjectToViewItem(ParserAliasDto source, ICollection<ComboBoxItem> employees)
        {
            return new ParserAliasViewItem
            {
                Id = source.Id,
                StateId = source.StateId,
                ProductId = source.ProductId,
                Name = source.Name,
                PartNumber = source.PartNumber,
                CreatedOn = source.CreatedOn,
                ModifiedOn = source.ModifiedOn,
                ModifiedById = source.ModifiedById,
                ModifiedByDisplayString = source.ModifiedOn.HasValue
                    ? $"{employees.FirstOrDefault(x => x.Id == source.ModifiedById).DisplayValue} ({source.ModifiedOn?.ToString(DateFormattingRules.FullDateTimeFormat)})"
                    : string.Empty,
                PostponedTo = source.PostponedTo,
                ComparsionResult = null
            };
        }

        private void SetData(ProductCardDto dto)
        {
            ProductCard = Mapper.Map<ProductCardViewItem>(dto);
            SelectedActiveValue = ActiveValues.FirstOrDefault(x => x.Id.Equals(ProductCard.Active));
        }

        private void CreateBarcode()
        {
            DialogDocumentManagerService.ShowView<ProductBarcodeCreateViewModel>(new ProductBarcodeCreateParameter(productId), this);
        }

        private void AddSerialNumberLengthByScan()
        {
            GetTextFromUserParameter fromUserParameter = new GetTextFromUserParameter(
                "SN длиной 5-100 символов",
                "Просканируйте SN",
                @"^[a-zA-Z0-9_-]{5,100}$",
                "Не валидное значение. ");

            GetTextFromUserViewModel fromUserViewModel = DialogDocumentManagerService.ShowView<GetTextFromUserViewModel>(fromUserParameter, this);

            if (!fromUserViewModel.IsOk)
            {
                return;
            }

            int length = fromUserViewModel.Content.Length;

            if (ProductCard.SerialNumberLength.Select(x => x.Length).Contains(length))
            {
                MessageFacadeService.ShowNotificationWarning("Указанная длина SN уже добавлена");
                return;
            }

            AddSerialNumberLengthInternal(length);

            MessageFacadeService.ShowNotificationInfo($"Длина SN {length} {WordEndingHelper.GetWordByNumber(length, "символ", "символа", "символов")} успешно добавлена");
        }

        private void AddSerialNumberLength()
        {
            GetTextFromUserParameter fromUserParameter = new GetTextFromUserParameter(
                "Целое число 5-100",
                "Укажите длину SN",
                @"^([5-9]|[1-8][0-9]|9[0-9]|100)$",
                "Не валидное значение. ");

            GetTextFromUserViewModel fromUserViewModel = DialogDocumentManagerService.ShowView<GetTextFromUserViewModel>(fromUserParameter, this);

            if (!fromUserViewModel.IsOk)
            {
                return;
            }

            int length = int.Parse(fromUserViewModel.Content);

            if (ProductCard.SerialNumberLength.Select(x => x.Length).Contains(length))
            {
                MessageFacadeService.ShowNotificationError("Указанная длина SN уже добавлена");
                return;
            }

            AddSerialNumberLengthInternal(length);
        }

        private void OnProductBarcodeMessage(ProductBarcodeMessage message)
        {
            if (message.Entity.ProductId != productId)
            {
                return;
            }

            switch (message.MessageType)
            {
                case MessageType.Added:
                    ProductCard.Barcodes.Insert(0, Mapper.Map<ProductBarcodeViewItem>(message.Entity));
                    break;
                case MessageType.Changed:
                    ProductCard.Barcodes.DoActionWithItem(x => x.Id == message.Entity.Id, viewItem => Mapper.Map(message.Entity, viewItem));
                    break;
            }
        }

        private void HandleSelectionChanged(ValueChangedEventArgs<FrameworkElement> e)
        {
            switch (e.NewValue.Name)
            {
                case "Tab1":
                    IsHelpVisible = false;
                    break;
                case "Tab3":
                    if (ParserAliases == null)
                    {
                        RefreshParserAliasesCommand.Execute(null);
                    }

                    IsHelpVisible = true;
                    break;
                case "Tab4":
                    LoadProductInformation();

                    IsHelpVisible = false;
                    break;
                default:
                    IsHelpVisible = false;
                    break;
            }
        }

        private void Move()
        {
            ProductMoveViewModel viewModel = SizeableDialogDocumentManagerService.ShowView<ProductMoveViewModel>(ProductCard, this);

            if (viewModel.IsOk)
            {
                SetData(viewModel.Result.Data);
            }
        }

        private async Task PrintBarcodeAsync()
        {
            PrintBarcodeParametersParameter parameter = new PrintBarcodeParametersParameter(1, 1);

            PrintBarcodeParametersViewModel viewModel = DialogDocumentManagerService.ShowView<PrintBarcodeParametersViewModel>(parameter, this);

            if (viewModel.IsOk)
            {
                short copies = (short)viewModel.Count;
                int quantity = (int)viewModel.Quantity;

                BarcodeReportFactoryResult result = await BarcodeReportFactory.CreateAsync(viewModel.Format, ProductCard.NameFullUa ?? ProductCard.Name, ProductCard.ProductId, quantity);

                if (result.Printer == null)
                {
                    MessageFacadeService.ShowNotificationError("Сначала задайте принтеры в настройках");
                    return;
                }

                PrintReportRequest printReportRequest = new PrintReportRequest(
                    result.Report,
                    false,
                    result.Printer.Name,
                    result.Printer.PaperSource,
                    copies);

                await Mediator.Send(printReportRequest);
            }
        }

        private async Task PrintImportStickerAsync()
        {
            PrintingSettingsInfo printingSettingsInfo = await PrintingSettings.LoadAsync();

            PrinterSettingsInfo printerSettings = printingSettingsInfo?.Barcode50X40;

            if (string.IsNullOrEmpty(printerSettings?.Name))
            {
                MessageFacadeService.ShowNotificationError("Сначала задайте принтеры в настройках");
                return;
            }

            CategoryFullDto category =
                await WebClient.ExecuteApiRequestAsync(new QueryCategoryFull(ProductCard.CategoryId));

            if (!CanPrintImportSticker(category))
            {
                return;
            }

            string nameFeature = string.Empty;

            if (category.FeatureId.HasValue)
            {
                FeatureFullDto featureFullDto =
                    await WebClient.ExecuteApiRequestAsync(new QueryFeature(category.FeatureId.Value));

                if (featureFullDto == null)
                {
                    MessageFacadeService.ShowNotificationWarning($"Характеристика №{category.FeatureId} указанная в категории {category.NameFull} не найдена");
                    return;
                }

                List<ProductFeatureGroupsDto> productFeatureGroupsDtos = await WebClient.ExecuteApiRequestAsync(new QueryProductFeatures(new[] { productId }));

                ReadOnlyObservableCollection<ProductFeatureDto> productFeatureDtos = productFeatureGroupsDtos?
                    .SelectMany(x => x.AttributeGroups)
                    .SelectMany(x => x.Attributes)
                    .ToReadOnlyObservableCollection();

                ProductFeatureDto productFeatureDto = productFeatureDtos?.FirstOrDefault(x => x.Id == featureFullDto.Id);

                string nameFeatureValue = productFeatureDto?.ValueUkr;

                nameFeature = $"{featureFullDto.NameUkr} : {nameFeatureValue}";
            }

            PrintImportStickerParametersParameter parameter = new PrintImportStickerParametersParameter(1);

            PrintImportStickerParametersViewModel viewModel =
                DialogDocumentManagerService.ShowView<PrintImportStickerParametersViewModel>(parameter, this);

            if (viewModel.IsOk)
            {
                short copies = (short)viewModel.Count;

                TelemartAddressDto telemartAddressDto = await WebClient.ExecuteApiRequestAsync(new QueryTelemartAddress());

                StickerProductReportData reportData = new StickerProductReportData(
                    ProductCard.NameFullUa,
                    nameFeature,
                    DateTime.Today.AddMonths(-2),
                    category.MarkerManufacture ?? string.Empty,
                    category.MarkerManufactureAddress ?? string.Empty,
                    "ТОВ \"ТЕЛЕМАРТ\"",
                    telemartAddressDto?.Address,
                    telemartAddressDto?.Phone,
                    telemartAddressDto?.Email);

                StickerProductReport report = new StickerProductReport
                {
                    DataSource = new[] { reportData }
                };

                PrintReportRequest printReportRequest = new PrintReportRequest(
                    report,
                    false,
                    printerSettings.Name,
                    printerSettings.PaperSource,
                    copies);

                await Mediator.Send(printReportRequest);
            }
        }

        private bool CanPrintImportSticker(CategoryFullDto category)
        {
            if (category == null)
            {
                MessageFacadeService.ShowNotificationWarning(
                    $"Категория не найдена");

                return false;
            }

            if (category.MarkerManufacture == null
                && category.MarkerManufactureAddress == null
                && category.FeatureId == null)
            {
                MessageFacadeService.ShowNotificationWarning(
                    $"В категории {category.NameFull}({category.Id}) не заполнено маркировка товара");

                return false;
            }

            if (string.IsNullOrEmpty(category.MarkerManufacture))
            {
                MessageFacadeService.ShowNotificationWarning(
                    $"В категории {category.NameFull}({category.Id}) не заполнено Название компании в блоке маркировка товара");

                return false;
            }

            if (string.IsNullOrEmpty(category.MarkerManufactureAddress))
            {
                MessageFacadeService.ShowNotificationWarning(
                    $"В категории {category.NameFull}({category.Id}) не заполнено Адрес в блоке маркировка товара");

                return false;
            }

            return true;
        }

        private void RemoveSerialNumberLength()
        {
            if (SelectedSerialNumberLength == null || !MessageFacadeService.Confirm("Вы уверены?"))
            {
                return;
            }

            ProductCard.SerialNumberLength.RemoveAll(x => x.Id == SelectedSerialNumberLength.Id);
        }

        private async Task RemoveBarcodeAsync()
        {
            if (SelectedBarcode == null || !MessageFacadeService.Confirm("Вы уверены?"))
            {
                return;
            }

            try
            {
                string barcode = SelectedBarcode.Barcode;

                int barcodeId = SelectedBarcode.Id;

                await WebClient.ExecuteApiRequestAsync(new DeleteProductBarcode(SelectedBarcode.Barcode));

                ProductCard.Barcodes.RemoveAll(x => x.Id == barcodeId);
                MessageFacadeService.ShowNotificationInfo($"Штрих-код {barcode} успешно удален");
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Error while deleting product barcode");
                MessageFacadeService.ShowNotificationError("Ошибка при удалении штрих-кода");
            }
        }

        private async Task RefreshParserAliasesAsync()
        {
            try
            {
                ParserAliases = null;

                ParserAliasFilteringItem filteringItem = new ParserAliasFilteringItem(null, ProductCard.ProductId, null, null, null);

                Task<List<ParserAliasDto>> getParserAliasesTask = WebClient.ExecuteApiRequestAsync(new QueryProductsParserAliases(filteringItem));
                Task refreshContractorsTask = RefreshContractorsAsync();
                Task refreshCategoriesTask = RefreshCategoriesAsync();

                await Task.WhenAll(getParserAliasesTask, refreshContractorsTask, refreshCategoriesTask);

                Dictionary<long, ParserAliasDto> aliasesDictionary = getParserAliasesTask.Result.ToDictionary(x => x.Id);

                ObservableCollection<ParserAliasViewItemWrapper> aliasesWithJoinedCategoriesAndContractors = new ObservableCollection<ParserAliasViewItemWrapper>();

                foreach (ParserAliasDto source in aliasesDictionary.Values)
                {
                    ParserAliasViewItem viewItem = MapTransferObjectToViewItem(source, Employees);

                    string categories = string.Join(Separator, source.CategoryIds.Where(y => categoriesDictionary.ContainsKey(y)).Select(y => categoriesDictionary[y]));
                    string contractors = string.Join(Separator, source.ContractorProducts.Where(y => contractorsDictionary.ContainsKey(y.ContractorId)).Select(y => contractorsDictionary[y.ContractorId]));

                    List<ContractorLinkViewItem> contractorLinkViewItems = source.ContractorProducts
                        .Where(y => contractorsDictionary.ContainsKey(y.ContractorId))
                        .Select(y => new ContractorLinkViewItem(y.ContractorId, contractorsDictionary[y.ContractorId], y.Link))
                        .ToList();

                    aliasesWithJoinedCategoriesAndContractors.Add(new ParserAliasViewItemWrapper(viewItem, categories, contractors, contractorLinkViewItems));
                }

                ParserAliases = aliasesWithJoinedCategoriesAndContractors;
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to refresh parser aliases");
                MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
            }
        }

        private async Task RefreshContractorsAsync()
        {
            if (contractorsDictionary == null)
            {
                List<ContractorDto> getContractors = await WebClient.ExecuteApiRequestAsync(new QueryContractors(), true).GetPagedResultDataAsync();

                contractorsDictionary = getContractors
                    .ToDictionary(x => x.Id, x => x.Name);
            }
        }

        private async Task RefreshEmployeesAsync()
        {
            List<EmployeeDto> employees = await WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true).GetPagedResultDataAsync();

            Employees = employees.Select(x => new ComboBoxItem(x.Id, x.Name)).ToReadOnlyObservableCollection();
        }

        private async Task RefreshCategoriesAsync()
        {
            if (categoriesDictionary == null)
            {
                List<CategoryDto> categories = await WebClient.ExecuteApiRequestAsync(new QueryCategories(), true).GetPagedResultDataAsync();

                categoriesDictionary = categories.ToDictionary(x => x.Id, y => y.Name);

                ProductDayCategories = categories
                    .Where(x => productDayCategoryIds.Contains(x.Id))
                    .OrderBy(x => x.Left)
                    .Select(x => new ComboBoxItem(x.Id, x.Name))
                    .ToReadOnlyObservableCollection();
            }
        }

        private async Task UnchainParserAliasAsync(ParserAliasViewItemWrapper viewItem)
        {
            try
            {
                if (MessageFacadeService.Confirm($"Сопоставление {viewItem.Name} будет отвязано от текущего товара, продолжить?"))
                {
                    List<ParserAliasSaveDto> toSave = new List<ParserAliasSaveDto> { new ParserAliasSaveDto { Id = viewItem.Id, StateId = (int)ParserAliasState.NotAssosiated, ProductId = null } };
                    await WebClient.ExecuteApiRequestAsync(new UpdateProductsParserAliases(toSave));

                    DeleteParserAlias(viewItem);
                    MessageFacadeService.ShowNotificationInfo($"Сопоставление {viewItem.Name} теперь не связано с этим товаром");
                }
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to save parser aliases");
                MessageFacadeService.ShowNotificationError("Ошибка при сохранении");
            }
        }

        private async Task RemoveParserAliasAsync(ParserAliasViewItemWrapper viewItem)
        {
            try
            {
                if (MessageFacadeService.Confirm($"Сопоставление {viewItem.Name} будет удалено, продолжить?"))
                {
                    await WebClient.ExecuteApiRequestAsync(new DeleteProductsParserAliases(new List<long> { viewItem.Id }));

                    DeleteParserAlias(viewItem);
                    MessageFacadeService.ShowNotificationInfo($"Сопоставление {viewItem.Name} успешно удалено");
                }
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to delete parser alias");
                MessageFacadeService.ShowNotificationError("Ошибка при удалении товара");
            }
        }

        private void DeleteParserAlias(ParserAliasViewItemWrapper viewItem)
        {
            for (int i = ParserAliases.Count - 1; i >= 0; i--)
            {
                if (ParserAliases[i].Id == viewItem.Id)
                {
                    ParserAliases.RemoveAt(i);
                }
            }
        }

        private void LoadProductInformation()
        {
            ProductInformation.ClearProduct();

            ProductInformation.ProductId = new ProductInfoId(ProductCard.ProductId, ProductCard.CurrencyId);
        }

        private void AddSerialNumberLengthInternal(int length)
        {
            ProductCard.SerialNumberLength.Add(new ProductSnLengthViewItem()
            {
                Length = length,
                CreatedBy = WebClient.AuthenticatedEmployee.Id,
                CreatedOn = DateTime.Now,
                ProductId = ProductCard.ProductId
            });
        }

        private async Task ClearCashAsync()
        {
            await ErrorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new CleanProductFromCash(ProductCard.ProductId)),
                "очишении кеша по товару",
                "Кеш очишен",
                this,
                true,
                confirmText: "Очистить кеш по товару");
        }
    }
}