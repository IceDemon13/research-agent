using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Telemart.Client.Business;
using Telemart.Client.Common;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.Requests.Features.ServiceCenter;
using Telemart.Client.Data.Requests.Features.ServiceInvoice;
using Telemart.Client.Data.Requests.Features.ServiceRepair;
using Telemart.Client.Data.Requests.Features.ServiceRepair.Actions;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Service.ServiceInvoices;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Service.ServiceRepairs
{
    internal sealed class ServiceRepairViewModel : TelemartEditorExViewModelBase<ServiceRepairDto, ServiceRepairViewMessage, ServiceRepairViewItem>
    {
        private IReadOnlyDictionary<int, string> employees;
        private IReadOnlyCollection<ServiceCenterDto> serviceCenters;
        private ServiceInvoiceViewItem _serviceInvoice;

        public ServiceRepairViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            IMessenger messenger,
            DocumentCommands documentCommands)
            : base(webClient, dictionaries, messageFacadeService, mapper, messenger)
        {
            DocumentCommands = documentCommands;

            CancelRepairCommand = new AsyncCommand(CancelRepairAsync, CanCancelRepair);
            TakeFromServiceCenterCommand = new AsyncCommand(TakeFromServiceCenterAsync, CanTakeFromServiceCenter);
            ShowServiceInvoiceCommand = new DelegateCommand<int?>(ShowServiceInvoice, x => x != null);
            ShowServiceRequestCommand = new DelegateCommand<int>(ShowServiceRequest);
            ShowServiceCenterCommand = new DelegateCommand<int?>(ShowServiceCenter, x => x != null);
            ConfirmCommand = new AsyncCommand(ConfirmAsync, () => Model?.State == ServiceRepairState.New);
            ReconfirmCommand = new AsyncCommand(ReconfirmAsync, () => Model?.State == ServiceRepairState.Confirmed);
        }

        public ServiceRepairViewModel()
        {
        }

        #region Commands

        public IAsyncCommand CancelRepairCommand { get; }

        public IAsyncCommand TakeFromServiceCenterCommand { get; }

        public IAsyncCommand ConfirmCommand { get; }

        public IAsyncCommand ReconfirmCommand { get; }

        public IDelegateCommand ShowServiceInvoiceCommand { get; }

        public IDelegateCommand ShowServiceRequestCommand { get; }

        public IDelegateCommand ShowServiceCenterCommand { get; }

        public DocumentCommands DocumentCommands { get; }

        #endregion

        #region INPC

        public ReadOnlyObservableCollection<ComboBoxItem> ServiceCenters
        {
            get { return GetProperty(() => ServiceCenters); }
            private set { SetProperty(() => ServiceCenters, value); }
        }

        public ComboBoxItem? SelectedServiceCenter
        {
            get { return GetProperty(() => SelectedServiceCenter); }
            set { SetProperty(() => SelectedServiceCenter, value, () => Model.ServiceCenterId = SelectedServiceCenter?.Id); }
        }

        public IEnumerable<SummaryViewItem> SummaryItems
        {
            get { return GetProperty(() => SummaryItems); }
            private set { SetProperty(() => SummaryItems, value); }
        }

        public IEnumerable<SummaryViewItem> ServiceInvoiceSummaryItems
        {
            get { return GetProperty(() => ServiceInvoiceSummaryItems); }
            private set { SetProperty(() => ServiceInvoiceSummaryItems, value, () => RaisePropertyChanged(nameof(IsVisibleServiceInvoiceInfo))); }
        }

        public bool IsVisibleServiceInvoiceInfo => ServiceInvoiceSummaryItems?.ToArray().Length > 0;

        #endregion

        protected override string CreatedActionMessage { get; } = "создан";

        protected override string EntityName { get; } = "Ремонт";

        protected override string UpdatedActionMessage { get; } = "сохранен";

        private bool IsAdminOrServiceManager => WebClient.AuthenticatedEmployee.HasAnyRole(Role.Admin, Role.ServiceManager);

        public static void BuildMetadata(MetadataBuilder<ServiceRepairViewModel> builder)
        {
            builder.Property(x => x.Model).Required();
        }

        protected override void OnInitializeInDesignMode()
        {
            base.OnInitializeInDesignMode();

            employees = new Dictionary<int, string>
            {
                [8] = "Сазонов Илья",
                [79] = "Правдивый Александр"
            };

            SummaryItems = GetSummaryItems(new ServiceRepairViewItem
            {
                Id = 666,
                ServiceRequestId = 999,
                State = ServiceRepairState.Confirmed,
                CreatedOn = DateTime.Now.AddDays(-7),
                CreatedBy = 79,
                ModifiedOn = DateTime.Now.AddDays(-2),
                ModifiedBy = 8,
                CompletedOn = DateTime.Now,
                CompletedBy = 79,
                RepairInvoice = "2345656007"
            });

            ServiceInvoiceSummaryItems = GetServiceInvoiceSummaryItems(new ServiceInvoiceViewItem());
        }

        protected override Task<Result<ServiceRepairDto>> CreateEntityAsync()
        {
            throw new NotImplementedException();
        }

        protected override object CreateEntityMessage(ServiceRepairDto dto, MessageType messageType)
        {
            return new ServiceRepairMessage(dto, messageType);
        }

        protected override Task<ServiceRepairDto> GetEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new QueryServiceRepair(id));
        }

        protected override Task<LockResponse<ServiceRepairDto>> LockEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new LockServiceRepair(id));
        }

        protected override Task<LockResponse<ServiceRepairDto>> UnlockEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new UnlockServiceRepair(id));
        }

        protected override void SetCreateTitle()
        {
            throw new NotImplementedException();
        }

        protected override void SetEditTitle()
        {
            Title = $"Ремонт №{Model.Id.ToString(CultureInfo.InvariantCulture)}";
        }

        protected override Task<Result<ServiceRepairDto>> UpdateEntityAsync()
        {
            UpdateServiceRepair gatewayRequest = new UpdateServiceRepair(Model.Id, Model.ServiceCenterId, Model.Defect, Model.Comment);
            return WebClient.ExecuteApiRequestAsync(gatewayRequest);
        }

        protected override bool CanEdit()
        {
            return Model?.State == ServiceRepairState.New;
        }

        protected override async Task HandleLoadedAsync()
        {
            await Task.WhenAll(
                RefreshEmployeesAsync(),
                RefreshServiceCentersAsync());

            await base.HandleLoadedAsync();

            await RefreshServiceInvoiceAsync(Model.ServiceInvoiceId);

            ServiceCenters = serviceCenters
                .Where(x => x.Active || x.Id == Model.ServiceCenterId)
                .OrderBy(x => x.Name)
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToReadOnlyObservableCollection();

            SelectedServiceCenter = ServiceCenters.Where(x => x.Id == Model.ServiceCenterId).Cast<ComboBoxItem?>().FirstOrDefault();

            RefreshSummaryItems();

            async Task RefreshEmployeesAsync()
            {
                IReadOnlyCollection<EmployeeDto> employeesList = await WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true).GetPagedResultDataAsync().ConfigureAwait(false);
                employees = employeesList.ToDictionary(x => x.Id, x => x.Name);
            }

            async Task RefreshServiceCentersAsync()
            {
                PagedResult<ServiceCenterDto> result = await WebClient.ExecuteApiRequestAsync(new QueryServiceCenters(), true);
                serviceCenters = result.Data;
            }

            async Task RefreshServiceInvoiceAsync(int? serviceInvoiceId)
            {
                if (serviceInvoiceId.HasValue)
                {
                    ServiceInvoiceDto serviceInvoiceDto = await WebClient.ExecuteApiRequestAsync(new QueryServiceInvoice(serviceInvoiceId.Value));

                    _serviceInvoice = Mapper.Map<ServiceInvoiceViewItem>(serviceInvoiceDto);
                }
            }
        }

        protected override void AfterSetData()
        {
            if (ServiceCenters != null)
            {
                SelectedServiceCenter = ServiceCenters?.Where(x => x.Id == Model.ServiceCenterId).Cast<ComboBoxItem?>().FirstOrDefault();
            }
        }

        private void RefreshSummaryItems()
        {
            SummaryItems = GetSummaryItems(Model);

            ServiceInvoiceSummaryItems = GetServiceInvoiceSummaryItems(_serviceInvoice);
        }

        private IEnumerable<SummaryViewItem> GetSummaryItems(ServiceRepairViewItem item)
        {
            yield return new SummaryViewItem("Номер", item.Id.ToString());
            yield return new SummaryViewItem("Создал", $"{employees.GetValueOrDefault(item.CreatedBy)} ({item.CreatedOn.ToString(DateFormattingRules.FullDateTimeFormat)})");
            yield return new SummaryViewItem("Изменил", $"{employees.GetValueOrDefault(item.ModifiedBy)} ({item.ModifiedOn.ToString(DateFormattingRules.FullDateTimeFormat)})");

            if (item.CompletedOn.HasValue && item.CompletedBy.HasValue)
            {
                yield return new SummaryViewItem("Завершил", $"{employees.GetValueOrDefault(item.CompletedBy.Value)} ({item.CompletedOn.Value.ToString(DateFormattingRules.FullDateTimeFormat)})");
            }

            yield return new SummaryViewItem("Статус", item.State.Name);

            if (!string.IsNullOrWhiteSpace(item.RepairInvoice))
            {
                yield return new SummaryViewItem("Сохранка", item.RepairInvoice);
            }
        }

        private IEnumerable<SummaryViewItem> GetServiceInvoiceSummaryItems(ServiceInvoiceViewItem serviceInvoice)
        {
            if (serviceInvoice == null)
            {
                yield break;
            }

            yield return new SummaryViewItem("Доставка", serviceInvoice.CarryType?.Name);
            yield return new SummaryViewItem("Статус", serviceInvoice.State?.Name);

            if (serviceInvoice.EmployeeCarrierId.HasValue && employees.TryGetValue(serviceInvoice.EmployeeCarrierId.Value, out string employeeName))
            {
                yield return new SummaryViewItem("Водитель", employeeName);
            }

            if (employees.TryGetValue(serviceInvoice.CreatedById, out string createdByEmployeeName))
            {
                yield return new SummaryViewItem("Создал", $"{createdByEmployeeName} ({serviceInvoice.CreatedOn.ToString(DateFormattingRules.FullDateTimeFormat)})");
            }

            if (serviceInvoice.SendDate.HasValue)
            {
                yield return new SummaryViewItem("Отправка", serviceInvoice.SendDate.Value.ToString(DateFormattingRules.DateFormat));
            }

            if (serviceInvoice.SentOn.HasValue)
            {
                yield return new SummaryViewItem("Отправлена", serviceInvoice.SentOn.Value.ToString(DateFormattingRules.FullDateTimeFormat));
            }

            if (!string.IsNullOrEmpty(serviceInvoice.Ttn))
            {
                yield return new SummaryViewItem("ТТН", serviceInvoice.Ttn);
            }
        }

        private void ShowServiceInvoice(int? serviceInvoiceId)
        {
            if (serviceInvoiceId.HasValue)
            {
                Messenger.Send(new ServiceInvoiceViewMessage(serviceInvoiceId.Value));
            }
        }

        private void ShowServiceCenter(int? serviceCenterId)
        {
            if (serviceCenterId.HasValue)
            {
                Messenger.Send(new ServiceCenterViewMessage(serviceCenterId.Value));
            }
        }

        private void ShowServiceRequest(int serviceRequestId)
        {
            Messenger.Send(new ServiceRequestViewMessage(serviceRequestId));
        }

        private bool CanTakeFromServiceCenter()
        {
            return Model != null
                && Model.State == ServiceRepairState.InServiceCenter
                && IsAdminOrServiceManager
                && Model?.EmployeeLockId == null;
        }

        private Task TakeFromServiceCenterAsync()
        {
            return ExecuteLockableOperationAsync(_ =>
            {
                DialogDocumentManagerService.ShowView<TakeFromServiceCenterViewModel>(Model, this);
            });
        }

        private bool CanCancelRepair()
        {
            return Model != null
                && (Model.State == ServiceRepairState.New || Model.State == ServiceRepairState.Confirmed)
                && Model?.EmployeeLockId == null;
        }

        private Task CancelRepairAsync()
        {
            if (!MessageFacadeService.Confirm("Вы уверены, что хотите отменить ремонт?"))
            {
                return Task.CompletedTask;
            }

            return ExecuteLockableOperationAsync(async lockedEntity =>
            {
                CancelServiceRepair gatewayRequest = new CancelServiceRepair(lockedEntity.Id);
                Result<ServiceRepairDto> result = await WebClient.ExecuteApiRequestAsync(gatewayRequest);
                SetData(result.Data);
                Messenger.Send(new ServiceRepairWorkflowMessage(result.Data));
            });
        }

        private Task ConfirmAsync()
        {
            if (!MessageFacadeService.Confirm($"Вы подтверждаете изменение статуса ремонта на \"{ServiceRepairState.Confirmed.Name}\"?"))
            {
                return Task.CompletedTask;
            }

            return ExecuteLockableOperationAsync(async lockedEntity =>
            {
                ConfirmServiceRepair gatewayRequest = new ConfirmServiceRepair(lockedEntity.Id);
                Result<ServiceRepairDto> result = await WebClient.ExecuteApiRequestAsync(gatewayRequest);
                SetData(result.Data);
            });
        }

        private Task ReconfirmAsync()
        {
            if (!MessageFacadeService.Confirm($"Вы подтверждаете изменение статуса ремонта на \"{ServiceRepairState.New.Name}\"?"))
            {
                return Task.CompletedTask;
            }

            return ExecuteLockableOperationAsync(async lockedEntity =>
            {
                ReconfirmServiceRepair gatewayRequest = new ReconfirmServiceRepair(lockedEntity.Id);
                Result<ServiceRepairDto> result = await WebClient.ExecuteApiRequestAsync(gatewayRequest);
                SetData(result.Data);
            });
        }
    }
}