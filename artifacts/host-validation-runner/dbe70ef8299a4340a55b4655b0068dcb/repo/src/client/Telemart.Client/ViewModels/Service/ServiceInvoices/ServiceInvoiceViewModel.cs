using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Xpf.Grid;
using DevExpress.XtraPrinting;
using MediatR;
using Microsoft.Extensions.Logging;
using Telemart.Client.Business;
using Telemart.Client.Common;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Settings.Printing;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.Requests.Features.ServiceCenter;
using Telemart.Client.Data.Requests.Features.ServiceInvoice;
using Telemart.Client.Data.Requests.Features.ServiceInvoice.Actions;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Mediator.Requests;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Warehouse;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Service.ServiceInvoices
{
    public sealed class ServiceInvoiceViewModel : TelemartEditorViewModelBase<ServiceInvoiceDto, ServiceInvoiceViewMessage, ServiceInvoiceViewItem>
    {
        private List<ServiceCenterDto> serviceCenters;

        private List<EmployeeDto> employees;

        private IReadOnlyDictionary<int, string> warehouseNamesDictionary;

        public ServiceInvoiceViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            IMessenger messenger,
            IPrintingSettingsStore printingSettingsStore,
            IMediator mediator)
            : base(webClient, dictionaries, messageFacadeService, mapper, messenger)
        {
            SendServiceInvoiceCommand = new AsyncCommand(SendServiceInvoiceAsync);
            CompleteServiceInvoiceCommand = new AsyncCommand(CompleteServiceInvoiceAsync);
            DeleteProductCommand = new DelegateCommand<ServiceInvoiceProductViewItem>(DeleteProduct, x => x != null);
            ExportToXlsxCommand = new DelegateCommand<TableView>(ExportToXlsx);
            PrintCommand = new AsyncCommand(PrintAsync);
            PrintTrackNumberCommand = new AsyncCommand(PrintTrackNumberAsync);
            HandleRowDoubleClickCommand = new DelegateCommand<RowDoubleClickEventArgs>(HandleRowDoubleClick);

            PrintingSettingsStore = printingSettingsStore;
            Mediator = mediator;
        }

        public ServiceInvoiceViewModel()
        {
        }

        #region Commands

        public IAsyncCommand CompleteServiceInvoiceCommand { get; }

        public IAsyncCommand SendServiceInvoiceCommand { get; }

        public IDelegateCommand DeleteProductCommand { get; }

        public IDelegateCommand ExportToXlsxCommand { get; }

        public IDelegateCommand HandleRowDoubleClickCommand { get; }

        public IAsyncCommand PrintCommand { get; }

        public IAsyncCommand PrintTrackNumberCommand { get; }

        #endregion

        #region INPC

        public IEnumerable<SummaryViewItem> SummaryItems
        {
            get { return GetProperty(() => SummaryItems); }
            private set { SetProperty(() => SummaryItems, value); }
        }

        public ServiceInvoiceProductViewItem SelectedProduct
        {
            get { return GetProperty(() => SelectedProduct); }
            set { SetProperty(() => SelectedProduct, value); }
        }

        #endregion

        #region DialogSettings

        public override int Height => 640;

        public override int MinHeight => 640;

        public override int MinWidth => 1024;

        public override int Width => 1024;

        #endregion

        public bool IsInvoiceCompletionAllowed => WebClient.AuthenticatedEmployee.HasAnyRole(Role.Admin, Role.TechSupport, Role.ServiceManager);

        public bool IsAllowEdit => WebClient.AuthenticatedEmployee.HasAnyRole(Role.Admin, Role.ServiceManager);

        protected override string CreatedActionMessage { get; } = "создана";

        protected override string EntityName { get; } = "Серв. накладная";

        protected override string UpdatedActionMessage { get; } = "сохранена";

        private IPrintingSettingsStore PrintingSettingsStore { get; }

        private IMediator Mediator { get; }

        private ISaveFileDialogService SaveFileDialogService => GetService<ISaveFileDialogService>("ExcelSaveFileDialogService", ServiceSearchMode.PreferParents);

        protected override Task<Result<ServiceInvoiceDto>> CreateEntityAsync()
        {
            throw new NotSupportedException();
        }

        protected override object CreateEntityMessage(ServiceInvoiceDto dto, MessageType messageType)
        {
            return new ServiceInvoiceMessage(dto, messageType);
        }

        protected override Task<ServiceInvoiceDto> GetEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new QueryServiceInvoice(id));
        }

        protected override void AfterSetData()
        {
            Model.Products = Model.Products.OrderBy(x => x.ProductName).ToObservableRangeCollection();
            RefreshSummaryItems();
        }

        protected override Task<LockResponse<ServiceInvoiceDto>> LockEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new LockServiceInvoice(id));
        }

        protected override Task<LockResponse<ServiceInvoiceDto>> UnlockEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new UnlockServiceInvoice(id));
        }

        protected override void SetCreateTitle()
        {
        }

        protected override void SetEditTitle()
        {
            Title = $"Серв. накладная №{Model.Id}";
        }

        protected override Task<Result<ServiceInvoiceDto>> UpdateEntityAsync()
        {
            ServiceInvoiceSaveDto saveDto = new ServiceInvoiceSaveDto
            {
                Products = Model.Products.Select(x => Mapper.Map<ServiceInvoiceProductDto>(x)).ToList()
            };

            return WebClient.ExecuteApiRequestAsync(new UpdateServiceInvoice(Model.Id, saveDto));
        }

        protected override async Task HandleLoadedAsync()
        {
            serviceCenters = await WebClient.ExecuteApiRequestAsync(new QueryServiceCenters()).GetPagedResultDataAsync();
            employees = await WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true).GetPagedResultDataAsync();

            List<WarehouseDto> warehouses = await WebClient.ExecuteApiRequestAsync(new QueryWarehouses(), true).GetPagedResultDataAsync();

            warehouseNamesDictionary = warehouses.ToDictionary(x => x.Id, y => y.Name);

            await base.HandleLoadedAsync();
        }

        protected override IEnumerable<string> GetMembersToIgnore()
        {
            yield return nameof(Model.Products);
        }

        private void RefreshSummaryItems()
        {
            SummaryItems = GetSummaryItems();
        }

        private IEnumerable<SummaryViewItem> GetSummaryItems()
        {
            ServiceCenterDto serviceCenter = serviceCenters.FirstOrDefault(x => x.Id == Model.ServiceCenterId);

            yield return new SummaryViewItem("СЦ", serviceCenter?.Name);

            if (!string.IsNullOrWhiteSpace(serviceCenter?.Address))
            {
                yield return new SummaryViewItem("Адрес", serviceCenter.Address);
            }

            yield return new SummaryViewItem("Склад", warehouseNamesDictionary.GetValueOrDefault(Model.WarehouseId));
            yield return new SummaryViewItem("Доставка", Model.CarryType.Name);
            yield return new SummaryViewItem("Статус", Model.State?.Name);

            if (Model.EmployeeCarrierId.HasValue)
            {
                yield return new SummaryViewItem("Водитель", employees.FirstOrDefault(x => x.Id == Model.EmployeeCarrierId)?.Name);
            }

            string createdByEmployeeName = employees.FirstOrDefault(x => x.Id == Model.CreatedById)?.Name;
            yield return new SummaryViewItem("Создал", $"{createdByEmployeeName} ({Model.CreatedOn.ToString(DateFormattingRules.FullDateTimeFormat)})");
            yield return new SummaryViewItem("Отправка", Model.SendDate.Value.ToString(DateFormattingRules.DateFormat));

            if (Model.SentOn.HasValue)
            {
                yield return new SummaryViewItem("Отправлена", Model.SentOn.Value.ToString(DateFormattingRules.FullDateTimeFormat));
            }

            if (!string.IsNullOrEmpty(Model.Ttn))
            {
                yield return new SummaryViewItem("ТТН", Model.Ttn);
            }
        }

        private Task SendServiceInvoiceAsync()
        {
            return ExecuteLockableOperationAsync(lockedEntity =>
            {
                SendServiceInvoiceParameter parameter = new SendServiceInvoiceParameter(
                    Model.Id,
                    Model.WarehouseId,
                    Model.EmployeeCarrierId,
                    Model.Ttn,
                    Model.CarryType.Id != CarryType.PickupId);

                DialogDocumentManagerService.ShowView<SendServiceInvoiceViewModel>(parameter, this);
            });
        }

        private Task CompleteServiceInvoiceAsync()
        {
            return ExecuteLockableOperationAsync(
                lockedEntity =>
                {
                    CompleteServiceInvoiceParameter parameter = new CompleteServiceInvoiceParameter(Model.Id, Model.Products);
                    DialogDocumentManagerService.ShowView<CompleteServiceInvoiceViewModel>(parameter, this);
                });
        }

        private void ExportToXlsx(TableView tableView)
        {
            if (tableView?.Grid == null)
            {
                return;
            }

            string fileName = $"Service_Invoice_{Model.Id}_{DateTime.Now:yyyy-MM-dd}";
            string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            SaveFileDialogService.ShowDialog(
                x =>
                {
                    string filePath = SaveFileDialogService.File.GetFullName();
                    XlsxExportOptionsEx options = new XlsxExportOptionsEx(TextExportMode.Text);
                    tableView.ExportToXlsx(filePath, options);
                    MessageFacadeService.ShowNotificationInfo("Данные успешно сохранены");
                },
                folderPath,
                fileName);
        }

        private void DeleteProduct(ServiceInvoiceProductViewItem product)
        {
            Model.Products.Remove(product);
            RaisePropertyChanged(nameof(IsChanged));
        }

        private async Task PrintAsync()
        {
            try
            {
                await Mediator.Send(new PrintServiceInvoiceReportRequest(Model.Id));
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при печати");
                ShowValidationResultView("Ошибки при печати", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to print");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при печати");
                Logger.LogError(exception, "Error while printing");
            }
        }

        private Task PrintTrackNumberAsync()
        {
            return Model.CarryType
            .GetTrackNumberProvider()
                .PrintAsync(Model.Ttn, true);
        }

        private void HandleRowDoubleClick(RowDoubleClickEventArgs args)
        {
            ServiceInvoiceProductViewItem item = (ServiceInvoiceProductViewItem)((GridControl)args.Source.DataControl).CurrentItem;

            switch (args.HitInfo.Column.FieldName)
            {
                case nameof(item.ServiceRepairId):
                    Messenger.Send(new ServiceRepairViewMessage(item.ServiceRepairId));
                    break;
                case nameof(item.ServiceRequestId):
                    Messenger.Send(new ServiceRequestViewMessage(item.ServiceRequestId));
                    break;
            }
        }
    }
}