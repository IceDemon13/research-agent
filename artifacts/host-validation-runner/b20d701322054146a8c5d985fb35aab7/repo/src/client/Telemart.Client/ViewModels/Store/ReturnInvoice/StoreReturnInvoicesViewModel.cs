using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.XtraReports;
using MediatR;
using Microsoft.Extensions.Logging;
using Telemart.Client.Business;
using Telemart.Client.Business.Delivery;
using Telemart.Client.Common;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Settings.Printing;
using Telemart.Client.Data.Requests.Features.Carry;
using Telemart.Client.Data.Requests.Features.Contractor;
using Telemart.Client.Data.Requests.Features.PrintReport;
using Telemart.Client.Data.Requests.Features.ReturnInvoice;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Mediator.Requests;
using Telemart.Client.Properties;
using Telemart.Client.Reports.ReturnInvoice;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Carry;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.TransferObjects.ReturnInvoice;
using Telemart.Client.TransferObjects.Warehouse;
using Telemart.Client.ViewModels.Dialogs;
using Telemart.Client.ViewModels.Nomenclature;

namespace Telemart.Client.ViewModels.Store.ReturnInvoice
{
    public sealed class StoreReturnInvoicesViewModel : ViewModelBase, ISupportHotkeys
    {
        public StoreReturnInvoicesViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessenger messenger,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            DocumentCommands documentCommands,
            IMediator mediator,
            IPrintingSettingsStore printingSettingsStore,
            ILogger<StoreReturnInvoicesViewModel> logger)
            : this()
        {
            PrintingSettingsStore = printingSettingsStore ?? throw new ArgumentNullException(nameof(printingSettingsStore));
            Mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            WebClient = webClient ?? throw new ArgumentNullException(nameof(webClient));
            Mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            Messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
            MessageFacadeService = messageFacadeService ?? throw new ArgumentNullException(nameof(messageFacadeService));
            Logger = logger;

            Filter = new StoreReturnInvoicesFilterViewModel(dictionaries, webClient);

            Messenger.Register<ReturnInvoiceMessage>(this, OnReturnInvoiceMessage);
            Messenger.Register<ReturnInvoicesMessage>(this, OnReturnInvoicesMessage);
            Dictionaries = dictionaries;
            DocumentCommands = documentCommands;
        }

        public StoreReturnInvoicesViewModel()
        {
            RefreshCommand = new AsyncCommand(RefreshAsync);
            PrintSnCommand = new AsyncCommand(PrintSnAsync, () => SelectedReturnInvoice != null && SelectedReturnInvoice.StateId != ReturnInvoiceState.Canceled.Id && SelectedReturnInvoice.StateId != ReturnInvoiceState.Sent.Id);
            CancelFilteringCommand = new DelegateCommand(CancelFiltering);
            HandleLoadedCommand = new DelegateCommand(HandleLoaded);
            EditCommand = new DelegateCommand<ReturnInvoiceViewItem>(Edit, CanEdit);
            CreateReturnInvoiceCommand = new DelegateCommand(CreateReturnInvoice);
            CreateReturnInvoicesCommand = new DelegateCommand(CreateReturnInvoices);
            PrintReturnInvoiceAssemblyCommand = new AsyncCommand(PrintReturnInvoiceAssemblyAsync, () => SelectedReturnInvoice != null);
            PrintReturnInvoiceSupplierCommand = new AsyncCommand(PrintReturnInvoiceSupplierAsync, () => SelectedReturnInvoice != null);
            PrintTtnCommand = new AsyncCommand<ReturnInvoiceViewItem>(PrintTrackNumberAsync, x => !string.IsNullOrEmpty(x?.TrackNumber));
            ChangeAddressCommand = new DelegateCommand(ChangeAddress, ChangeCanAddress);
            SelectProductCommand = new DelegateCommand(SelectProduct);
        }

        public StoreReturnInvoicesFilterViewModel Filter { get; }

        #region Commands

        public IAsyncCommand RefreshCommand { get; }

        public IAsyncCommand PrintSnCommand { get; }

        public IAsyncCommand PrintReturnInvoiceAssemblyCommand { get; }

        public IAsyncCommand PrintReturnInvoiceSupplierCommand { get; }

        public IDelegateCommand CreateReturnInvoiceCommand { get; }

        public IDelegateCommand CreateReturnInvoicesCommand { get; }

        public IDelegateCommand CancelFilteringCommand { get; }

        public IDelegateCommand HandleLoadedCommand { get; }

        public IDelegateCommand EditCommand { get; }

        public IAsyncCommand PrintTtnCommand { get; }

        public IDelegateCommand ChangeAddressCommand { get; }

        public IDelegateCommand SelectProductCommand { get; }

        #endregion

        #region INPC

        public List<ContractorDto> Contractors
        {
            get { return GetProperty(() => Contractors); }
            private set { SetProperty(() => Contractors, value); }
        }

        public bool IsLongOperationInProgress
        {
            get { return GetProperty(() => IsLongOperationInProgress); }
            set { SetProperty(() => IsLongOperationInProgress, value); }
        }

        public ObservableCollection<ReturnInvoiceViewItem> ReturnInvoices
        {
            get { return GetProperty(() => ReturnInvoices); }
            set { SetProperty(() => ReturnInvoices, value); }
        }

        public ReturnInvoiceViewItem SelectedReturnInvoice
        {
            get { return GetProperty(() => SelectedReturnInvoice); }
            set { SetProperty(() => SelectedReturnInvoice, value); }
        }

        public ReturnInvoiceViewItem CurrentReturnInvoice
        {
            get { return GetProperty(() => CurrentReturnInvoice); }
            set { SetProperty(() => CurrentReturnInvoice, value); }
        }

        public List<WarehouseDto> Warehouses
        {
            get { return GetProperty(() => Warehouses); }
            private set { SetProperty(() => Warehouses, value); }
        }

        public List<CarryDto> Carries
        {
            get { return GetProperty(() => Carries); }
            private set { SetProperty(() => Carries, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> States
        {
            get { return GetProperty(() => States); }
            set { SetProperty(() => States, value); }
        }

        public bool IsColumnChooserVisible
        {
            get { return GetProperty(() => IsColumnChooserVisible); }
            set { SetProperty(() => IsColumnChooserVisible, value); }
        }

        public bool IsSearchPanelOpened
        {
            get { return GetProperty(() => IsSearchPanelOpened); }
            set { SetProperty(() => IsSearchPanelOpened, value); }
        }

        #endregion

        private IWebClient WebClient { get; }

        private IMessenger Messenger { get; }

        private IMapper Mapper { get; }

        private IMessageFacadeService MessageFacadeService { get; }

        private IDictionaries Dictionaries { get; }

        private IMediator Mediator { get; }

        private IPrintingSettingsStore PrintingSettingsStore { get; }

        private IDocumentManagerService SizeableDialogDocumentManagerService => GetService<IDocumentManagerService>("SizeableDialogDocumentManagerService", ServiceSearchMode.PreferParents);

        private IDocumentManagerService DialogDocumentManagerService => GetService<IDocumentManagerService>("DialogDocumentManagerService", ServiceSearchMode.PreferParents);

        private IDocumentManagerService NonModalDialogDocumentManagerService => GetService<IDocumentManagerService>("NonModalDialogDocumentManagerService");

        private IDocumentManagerService NonModalSizeableDialogDocumentManagerService => GetService<IDocumentManagerService>("NotModalSizeableDocumentManagerService");

        private ILogger<StoreReturnInvoicesViewModel> Logger { get; }

        private DocumentCommands DocumentCommands { get; }

        public bool HandleHotkey(HotkeyMessage msg)
        {
            bool handled = false;

            if (msg.ModifierKeys == ModifierKeys.Alt)
            {
                switch (msg.Key)
                {
                    case Key.L:
                        IsSearchPanelOpened = !IsSearchPanelOpened;
                        handled = true;
                        break;
                }
            }
            else
            {
                switch (msg.HotkeyMessageType)
                {
                    case HotkeyMessageType.Refresh:
                        RefreshCommand.Execute(null);
                        handled = true;
                        break;

                    case HotkeyMessageType.Edit:
                        EditCommand.Execute(CurrentReturnInvoice);
                        handled = true;
                        break;

                    case HotkeyMessageType.ShowColumnChooser:
                        IsColumnChooserVisible = !IsColumnChooserVisible;
                        handled = true;
                        break;

                    case HotkeyMessageType.Add:
                        CreateReturnInvoiceCommand.Execute(null);
                        handled = true;
                        break;
                }
            }

            return handled;
        }

        private async Task PrintSnAsync()
        {
            PrintingSettingsInfo printingSettings = await PrintingSettingsStore.LoadAsync();

            PrinterSettingsInfo printerSettings = printingSettings.Main;

            if (printerSettings == null)
            {
                MessageFacadeService.ShowNotificationError("Сначала задайте принтеры в настройках");
                return;
            }

            ReturnInvoiceReportDataDto dataDto = await WebClient.ExecuteApiRequestAsync(new QueryReturnInvoiceSerialsPrintReport(SelectedReturnInvoice.InvoiceId, SelectedReturnInvoice.Id));

            if (!dataDto.Products.Any())
            {
                MessageFacadeService.ShowNotificationWarning("Нет товара для печати");
                return;
            }

            ReturnInvoiceReportData data = Mapper.Map<ReturnInvoiceReportData>(dataDto);

            IReport report = new ReturnInvoiceReport()
            {
                DataSource = new[] { data }
            };

            PrintReportRequest request = new PrintReportRequest(report, true, printerSettings.Name, printerSettings.PaperSource);

            await Mediator.Send(request);
        }

        private async Task PrintReturnInvoiceAssemblyAsync()
        {
            PrintingSettingsInfo printingSettings = await PrintingSettingsStore.LoadAsync();

            PrinterSettingsInfo printerSettings = printingSettings.Main;

            if (printerSettings == null)
            {
                MessageFacadeService.ShowNotificationError("Сначала задайте принтеры в настройках");
                return;
            }

            ReturnInvoiceAssemblyReportData data = new ReturnInvoiceAssemblyReportData(
                SelectedReturnInvoice.Products
                    .GroupBy(x => new { x.CategoryId, x.CategoryName })
                    .Select(x => new ReturnInvoiceAssemblyReportCategoryData(x.Key.CategoryName, x.Select(z => new ReturnInvoiceAssemblyReportProductData(z.ProductId, z.FullName, z.Quantity)).ToList())).ToList(),
                Warehouses.FirstOrDefault(x => x.Id == SelectedReturnInvoice.WarehouseId)?.Name,
                Carries.FirstOrDefault(x => x.Id == SelectedReturnInvoice.CarryId)?.Name,
                SelectedReturnInvoice.SupplierName,
                SelectedReturnInvoice.Id);

            IReport report = new ReturnInvoiceAssemblyReport()
            {
                DataSource = new[] { data }
            };

            PrintReportRequest request = new PrintReportRequest(report, true, printerSettings.Name, printerSettings.PaperSource);

            await Mediator.Send(request);
        }

        private async Task PrintReturnInvoiceSupplierAsync()
        {
            PrintingSettingsInfo printingSettings = await PrintingSettingsStore.LoadAsync();

            PrinterSettingsInfo printerSettings = printingSettings.Main;

            if (printerSettings == null)
            {
                MessageFacadeService.ShowNotificationError("Сначала задайте принтеры в настройках");
                return;
            }

            ReturnInvoiceSupplierReportData data = new ReturnInvoiceSupplierReportData(
                SelectedReturnInvoice.Products
                    .Where(x => x.AcceptQuantity > 0)
                    .GroupBy(x => new { x.CategoryId, x.CategoryName })
                    .Select(x => new ReturnInvoiceSupplierReportCategoryData(
                        x.Key.CategoryName,
                        x.Select(z => new ReturnInvoiceSupplierReportProductData(
                            z.ProductId,
                            z.FullName,
                            z.AcceptQuantity.Value)).ToList())).ToList(),
                Warehouses.FirstOrDefault(x => x.Id == SelectedReturnInvoice.WarehouseId)?.Name,
                Carries.FirstOrDefault(x => x.Id == SelectedReturnInvoice.CarryId)?.Name,
                SelectedReturnInvoice.SupplierName,
                SelectedReturnInvoice.Id,
                SelectedReturnInvoice.CreatedOn.ToString(DateFormattingRules.DateFormat));

            IReport report = new ReturnInvoiceSupplierReport()
            {
                DataSource = new[] { data }
            };

            PrintReportRequest request = new PrintReportRequest(report, true, printerSettings.Name, printerSettings.PaperSource);

            await Mediator.Send(request);
        }

        private async Task RefreshAsync()
        {
            IsLongOperationInProgress = true;

            try
            {
                ReturnInvoices = null;
                Filter.ReturnInvoicesCount = 0;

                IFilteringItem filteringItem = Filter.GetInvoiceFilteringItem();

                Contractors = await WebClient.ExecuteApiRequestAsync(new QueryContractors(), true).GetPagedResultDataAsync();
                Warehouses = await WebClient.ExecuteApiRequestAsync(new QueryWarehouses(), true).GetPagedResultDataAsync();
                Carries = await WebClient.ExecuteApiRequestAsync(new QueryCarries(), true);

                States = Dictionaries
               .GetItems<ReturnInvoiceState>()
               .Select(x => new ComboBoxItem(x.Id, x.Name))
               .ToReadOnlyObservableCollection();

                await Filter.RefreshAsync();

                PagedResult<ReturnInvoiceDto> returnInvoices = await WebClient.ExecuteApiRequestAsync(new QueryReturnInvoices(filteringItem));

                ReturnInvoices = returnInvoices.Data.Select(x => Mapper.Map<ReturnInvoiceViewItem>(x)).ToObservableCollection();
                Filter.ReturnInvoicesCount = ReturnInvoices.Count;
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
                Logger.LogError(exception, "Exception while refreshing grid");
            }
            finally
            {
                IsLongOperationInProgress = false;
            }
        }

        private void HandleLoaded()
        {
            if (ReturnInvoices == null)
            {
                ReturnInvoices = new ObservableCollection<ReturnInvoiceViewItem>();
            }

            if (ReturnInvoices.Any())
            {
                return;
            }

            IsSearchPanelOpened = true;

            RefreshCommand.Execute(null);
        }

        private void CancelFiltering()
        {
            Filter.ResetFilterState();
            RefreshCommand.Execute(null);
        }

        private void Edit(ReturnInvoiceViewItem returnInvoiceViewItem)
        {
            NonModalSizeableDialogDocumentManagerService.ShowView<ReturnInvoiceViewModel>(new ReturnInvoiceParameter(SelectedReturnInvoice.Id), this);
        }

        private bool CanEdit(ReturnInvoiceViewItem returnInvoiceViewItem)
        {
            return SelectedReturnInvoice != null;
        }

        private void ChangeAddress()
        {
            DialogDocumentManagerService.ShowView<ChangeAddressReturnInvoiceViewModel>(new ChangeAddressReturnInvoiceParameter(SelectedReturnInvoice.Id), this);
        }

        private bool ChangeCanAddress()
        {
            return SelectedReturnInvoice != null
                   && WebClient.IsOperationAllowed(BusinessOperation.ReturnInvoiceChangeAddress)
                   && SelectedReturnInvoice.StateId != ReturnInvoiceState.Canceled.Id
                   && SelectedReturnInvoice.StateId != ReturnInvoiceState.Sent.Id;
        }

        private void CreateReturnInvoice()
        {
            GetTextFromUserViewModel viewModel = DialogDocumentManagerService.ShowView<GetTextFromUserViewModel>(
                    new GetTextFromUserParameter("Номер накладной", "Введите номер накладной", "^[0-9]{1,9}$", "Не валидное значение. "), this);

            if (!viewModel.IsOk)
            {
                return;
            }

            if (int.TryParse(viewModel.Content, out int invoiceId))
            {
                DocumentCommands.ShowCreateReturnInvoiceCommand.Execute(invoiceId);
            }
            else if (viewModel.IsOk)
            {
                MessageFacadeService.ShowMessageBoxError("Должно быть указано число");
            }
        }

        private void CreateReturnInvoices()
        {
            ManyReturnInvoicesViewModel createSeveralModel = DialogDocumentManagerService.ShowView<ManyReturnInvoicesViewModel>(null, this);

            if (!createSeveralModel.IsOk)
            {
                return;
            }

            ManyReturnInvoiceParameter severalReturnInvoiceParameter = createSeveralModel.GetSeveralReturnInvoiceParameter();

            NonModalDialogDocumentManagerService.ShowView<CreateManyReturnInvoicesViewModel>(severalReturnInvoiceParameter, this);
        }

        private async Task PrintTrackNumberAsync(ReturnInvoiceViewItem item)
        {
            if (!string.IsNullOrEmpty(item.TrackNumber))
            {
                ITrackNumberProvider trackNumberProvider = Dictionaries.GetItemById<CarryType>(item.CarryId).GetTrackNumberProvider();

                await trackNumberProvider.PrintAsync(item.TrackNumber, true);
            }
        }

        private void OnReturnInvoiceMessage(ReturnInvoiceMessage message)
        {
            ReturnInvoiceDto dto = message.Entity;

            switch (message.MessageType)
            {
                case MessageType.Added:
                    ReturnInvoices?.Add(Mapper.Map<ReturnInvoiceViewItem>(dto));
                    break;
                case MessageType.Changed:
                    ReturnInvoices?.DoActionWithItem(x => x.Id == dto.Id, x => Mapper.Map(dto, x));
                    break;
            }
        }

        private void OnReturnInvoicesMessage(ReturnInvoicesMessage message)
        {
            List<ReturnInvoiceViewItem> items = message.ReturnInvoiceDtos
                .Select(x => Mapper.Map<ReturnInvoiceViewItem>(x))
                .ToList();

            switch (message.MessageType)
            {
                case MessageType.Added:
                    items.ForEach(x => ReturnInvoices?.Add(x));
                    break;
            }
        }

        private void SelectProduct()
        {
            NomenclatureViewOptions options = new NomenclatureViewOptions(
                NomenclatureViewPriceContext.Client,
                Constants.TelemartContractorId,
                NomenclatureViewSelectionMode.Single,
                false);

            NomenclatureViewModel nomenclatureViewModel = SizeableDialogDocumentManagerService.ShowView<NomenclatureViewModel>(options, this);

            if (nomenclatureViewModel.IsOk)
            {
                NomenclatureViewItem product = nomenclatureViewModel.GetSelectedItems().First();

                Filter.ProductName = product.Name;
                Filter.ProductId = product.Id;
            }
        }
    }
}