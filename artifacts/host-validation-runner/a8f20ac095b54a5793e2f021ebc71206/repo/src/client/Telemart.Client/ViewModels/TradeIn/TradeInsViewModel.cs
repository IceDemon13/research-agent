using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Grid;
using DevExpress.XtraPrinting;
using DynamicData;
using Telemart.Client.Common;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Utils;
using Telemart.Client.Data.Requests.Features.Category;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.Requests.Features.TradeIn;
using Telemart.Client.Data.Requests.Features.TradeIn.Actions;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.Requests.Features.Warehouse.Delivery;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Reports.TradeIn;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.TransferObjects.TradeIn;
using Telemart.Client.TransferObjects.Warehouse;
using Telemart.Client.TransferObjects.Warehouse.Delivery;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Nomenclature;
using Telemart.Client.ViewModels.Store.CreateScanSheetForEntities;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.TradeIn
{
    public sealed class TradeInsViewModel : TelemartViewModelBase, ISupportHotkeys
    {
        private readonly IMessenger _messenger;
        private readonly IMapper _mapper;
        private readonly IErrorHandler _errorHandler;

        private IReadOnlyCollection<ComboBoxItem> _allCategories;

        public TradeInsViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            IMessenger messenger,
            IErrorHandler errorHandler)
            : base(webClient, dictionaries, messageFacadeService)
        {
            _mapper = mapper;
            _messenger = messenger;
            _errorHandler = errorHandler;

            Filter = new TradeInFilterViewModel(dictionaries, webClient);
            TradeInsViewItems = new ObservableRangeCollection<TradeInViewItem>();

            RefreshCommand = new AsyncCommand(RefreshAsync);
            CancelFilteringCommand = new DelegateCommand(CancelFiltering);
            EditCommand = new DelegateCommand(Edit, () => SelectedTradeInViewItem != null);
            CreateTradeInCommand = new DelegateCommand(CreateTradeIn, () => WebClient.IsOperationAllowed(BusinessOperation.CreateTradeIn));
            SelectProductCommand = new DelegateCommand(SelectProduct);
            ExportCommand = new DelegateCommand<TableView>(ExportToExcel);
            PrintActCommand = new AsyncCommand(PrintActAsync, () => SelectedTradeInViewItem != null && (SelectedTradeInViewItem.StateId == TradeInState.Received.Id || SelectedTradeInViewItem.StateId == TradeInState.Completed.Id));
            CreateScanSheetCommand = new AsyncCommand(CreateScanSheetAsync);
            CopyTradeInCommand = new DelegateCommand(CopyTradeIn, () => SelectedTradeInViewItem != null && WebClient.IsOperationAllowed(BusinessOperation.CopyTradeInDocument));
            OverReceiveCommand = new AsyncCommand(OverReceiveAsync, () => SelectedTradeInViewItem != null && SelectedTradeInViewItem.StateId == TradeInState.Received.Id && WebClient.IsOperationAllowed(BusinessOperation.OverReceiveTradeIn));

            _messenger.Register<TradeInMessage>(this, OnTradeInMessage);
        }

        public TradeInFilterViewModel Filter { get; }

        #region INPC

        public ReadOnlyObservableCollection<ComboBoxItem> Employees
        {
            get { return GetProperty(() => Employees); }
            set { SetProperty(() => Employees, value); }
        }

        public ReadOnlyObservableCollection<TradeInState> Statuses
        {
            get { return GetProperty(() => Statuses); }
            set { SetProperty(() => Statuses, value); }
        }

        public ReadOnlyObservableCollection<TradeInIndicatorValueDto> TradeInClassValues
        {
            get { return GetProperty(() => TradeInClassValues); }
            set { SetProperty(() => TradeInClassValues, value); }
        }

        public ReadOnlyObservableCollection<TradeInIndicatorValueDto> TradeInWarrantyValues
        {
            get { return GetProperty(() => TradeInWarrantyValues); }
            set { SetProperty(() => TradeInWarrantyValues, value); }
        }

        public ReadOnlyObservableCollection<TradeInIndicatorValueDto> TradeInPackageValues
        {
            get { return GetProperty(() => TradeInPackageValues); }
            set { SetProperty(() => TradeInPackageValues, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Warehouses
        {
            get { return GetProperty(() => Warehouses); }
            set { SetProperty(() => Warehouses, value); }
        }

        public ObservableCollection<TradeInViewItem> TradeInsViewItems
        {
            get { return GetProperty(() => TradeInsViewItems); }
            set { SetProperty(() => TradeInsViewItems, value); }
        }

        public TradeInViewItem SelectedTradeInViewItem
        {
            get { return GetProperty(() => SelectedTradeInViewItem); }
            set { SetProperty(() => SelectedTradeInViewItem, value); }
        }

        public bool IsColumnChooserVisible
        {
            get { return GetProperty(() => IsColumnChooserVisible); }
            set { SetProperty(() => IsColumnChooserVisible, value); }
        }

        public bool CanUseTools
        {
            get { return GetProperty(() => CanUseTools); }
            set { SetProperty(() => CanUseTools, value); }
        }

        #endregion

        #region Commands

        public IAsyncCommand RefreshCommand { get; }

        public IDelegateCommand CancelFilteringCommand { get; }

        public IDelegateCommand CreateTradeInCommand { get; }

        public IDelegateCommand EditCommand { get; }

        public IDelegateCommand ExportCommand { get; }

        public IDelegateCommand SelectProductCommand { get; }

        public IAsyncCommand PrintActCommand { get; }

        public IAsyncCommand CreateScanSheetCommand { get; }

        public IDelegateCommand CopyTradeInCommand { get; }

        public IAsyncCommand OverReceiveCommand { get; }

        #endregion

        private IDocumentManagerService SizeableDialogDocumentManagerService => GetService<IDocumentManagerService>("SizeableDialogDocumentManagerService", ServiceSearchMode.PreferParents);

        private IDocumentManagerService DialogDocumentManagerService => GetService<IDocumentManagerService>("DialogDocumentManagerService", ServiceSearchMode.PreferParents);

        private IDocumentManagerService NonModalDialogDocumentManagerService => GetService<IDocumentManagerService>("NonModalDialogDocumentManagerService");

        private ISaveFileDialogService SaveFileDialogService => GetService<ISaveFileDialogService>("ExcelSaveFileDialogService", ServiceSearchMode.PreferParents);

        private IDialogService WizardDialogService => GetService<IDialogService>("WizardDialogService", ServiceSearchMode.PreferParents);

        public bool HandleHotkey(HotkeyMessage hotkeyMessage)
        {
            bool handled = false;

            switch (hotkeyMessage.HotkeyMessageType)
            {
                case HotkeyMessageType.Refresh:
                    RefreshCommand.Execute(null);
                    handled = true;
                    break;

                case HotkeyMessageType.Edit:
                    EditCommand.Execute(null);
                    handled = true;
                    break;
                case HotkeyMessageType.ShowColumnChooser:
                    IsColumnChooserVisible = !IsColumnChooserVisible;
                    handled = true;
                    break;
            }

            return handled;
        }

        protected override async Task HandleLoadedAsync()
        {
            Statuses = Dictionaries.GetItems<TradeInState>().ToReadOnlyObservableCollection();

            TradeInsViewItems = new ObservableCollection<TradeInViewItem>();

            CanUseTools = WebClient.IsOperationAllowed(BusinessOperation.CopyTradeInDocument) || WebClient.IsOperationAllowed(BusinessOperation.OverReceiveTradeIn);

            await Task.WhenAll(LoadTradeInIndicatorValuesAsync(), LoadWarehousesAsync(), LoadCategoriesAsync());

            await Filter.RefreshAsync();

            RefreshCommand.Execute(null);
        }

        private async Task RefreshAsync()
        {
            await Task.WhenAll(LoadEmployeesAsync(), LoadTradeInsAsync(Filter.GetFilteringItem()));
        }

        private void Edit()
        {
            NonModalDialogDocumentManagerService.ShowEditorView<TradeInEditViewModel>(SelectedTradeInViewItem.Id, new TradeInViewMessage(SelectedTradeInViewItem.Id), this);
        }

        private void ExportToExcel(TableView tableView)
        {
            if (tableView?.Grid == null)
            {
                return;
            }

            string fileName = $"TradeIn_{DateTime.Now:yyyy-MM-dd}";
            string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            SaveFileDialogService.ShowDialog(
                _ =>
                {
                    string filePath = SaveFileDialogService.File.GetFullName();

                    tableView.ExportToXlsx(filePath, new XlsxExportOptionsEx(TextExportMode.Value));

                    MessageFacadeService.ShowNotificationInfo("Данные успешно сохранены");
                },
                folderPath,
                fileName);
        }

        private void CreateTradeIn()
        {
            DialogDocumentManagerService.ShowView<TradeInCreateViewModel>(null, this);
        }

        private void CancelFiltering()
        {
            Filter.ResetFilterValues();
            RefreshCommand.Execute(null);
        }

        private async Task LoadEmployeesAsync()
        {
            List<EmployeeDto> employees = await WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true).GetPagedResultDataAsync();

            Employees = employees.Where(x => x.Active)
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .OrderBy(x => x.DisplayValue)
                .ToReadOnlyObservableCollection();
        }

        private async Task LoadWarehousesAsync()
        {
            List<WarehouseDto> warehouseDtos = await _errorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new QueryWarehouses(), true).GetPagedResultDataAsync(),
                "получении списка складов",
                null,
                this,
                true,
                showNotification: false);

            Warehouses = warehouseDtos
                .Where(x => x.Active == 1 && (x.TypeId == WarehouseKind.Pickup.Id || x.TypeId == WarehouseKind.Service.Id))
                .OrderBy(x => x.Name)
                .Select(y => new ComboBoxItem(y.Id, y.Name))
                .ToReadOnlyObservableCollection();
        }

        private async Task LoadTradeInsAsync(TradeInFilteringItem filteringItem)
        {
            TradeInsViewItems.Clear();

            PagedResult<TradeInDto> dtosPagedResult = await _errorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new QueryTradeIns(filteringItem)),
                "получении Trade-In заявок",
                null,
                this,
                true,
                showDialog: false,
                showNotification: false);

            if (dtosPagedResult?.Data?.Count > 0)
            {
                TradeInsViewItems.AddRange(dtosPagedResult.Data.Select(x => _mapper.Map<TradeInViewItem>(x)));
            }
        }

        private async Task LoadTradeInIndicatorValuesAsync()
        {
            List<TradeInIndicatorValueDto> dtos = await _errorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new QueryTradeInIndicatorValues()),
                "получении параметров",
                null,
                this,
                true,
                showDialog: false,
                showNotification: false);

            if (dtos?.Count > 0)
            {
                TradeInClassValues = dtos.Where(x => x.IndicatorId == TradeInIndicator.Class.Id).ToReadOnlyObservableCollection();
                TradeInWarrantyValues = dtos.Where(x => x.IndicatorId == TradeInIndicator.Warranty.Id).ToReadOnlyObservableCollection();
                TradeInPackageValues = dtos.Where(x => x.IndicatorId == TradeInIndicator.Package.Id).ToReadOnlyObservableCollection();
            }
        }

        private async Task LoadCategoriesAsync()
        {
            PagedResult<CategoryDto> categories = await WebClient.ExecuteApiRequestAsync(new QueryCategories(), true);

            _allCategories = categories.Data.Select(x => new ComboBoxItem(x.Id, x.NameUkr)).ToArray();
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

        private void CopyTradeIn()
        {
            TradeInCreateViewModel model = DialogDocumentManagerService.ShowView<TradeInCreateViewModel>(new CreateTradeInParameter(SelectedTradeInViewItem!.Id), this);

            if (model.IsOk)
            {
                NonModalDialogDocumentManagerService.ShowEditorView<TradeInEditViewModel>(SelectedTradeInViewItem.Id, new TradeInViewMessage(model.TradeInId), this);
            }
        }

        private async Task OverReceiveAsync()
        {
            if (!MessageFacadeService.Confirm("Вы уверены, что хотите переоткрыть заявку?"))
            {
                return;
            }

            Result<TradeInDto> result = await _errorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new OverReceiveTradeIn(SelectedTradeInViewItem.Id)),
                "при перепринятии Trade-In заявки",
                "Trade-In заявка перепринята",
                this,
                false);

            if (result?.IsSuccess == true)
            {
                _messenger.Send(new TradeInMessage(result.Data, MessageType.Changed));
            }
        }

        private async Task PrintActAsync()
        {
            await TradeInReportPrinter.PrintActAsync(
                SelectedTradeInViewItem,
                TradeInWarrantyValues,
                TradeInPackageValues,
                _allCategories,
                WebClient);
        }

        private async Task CreateScanSheetAsync()
        {
            List<DeliveryDto> warehouseDeliveries = await WebClient.ExecuteApiRequestAsync(new QueryWarehouseDeliveries(), true);

            CreateScanSheetForEntitiesModel model = new CreateScanSheetForEntitiesModel(Entity.TradeInId);

            WizardDialogViewModel<CreateScanSheetForEntitiesModel> wizardDialogViewModel = new WizardDialogViewModel<CreateScanSheetForEntitiesModel>(
                typeof(SearchCriteriaPageViewModel),
                model,
                this);

            WizardDialogService.ShowDialog(MessageButton.OKCancel, "Создание реестра", wizardDialogViewModel);
        }

        private void OnTradeInMessage(TradeInMessage message)
        {
            switch (message?.MessageType)
            {
                case MessageType.Added:
                    TradeInsViewItems.Add(_mapper.Map<TradeInViewItem>(message.Entity));
                    break;
                case MessageType.Changed:
                    TradeInsViewItems.DoActionWithItem(x => x.Id == message.Entity.Id, viewItem => _mapper.Map(message.Entity, viewItem));
                    break;
            }
        }
    }
}