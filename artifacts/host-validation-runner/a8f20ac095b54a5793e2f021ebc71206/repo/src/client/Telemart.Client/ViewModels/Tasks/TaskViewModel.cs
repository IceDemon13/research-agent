using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using Telemart.Client.Business;
using Telemart.Client.Common;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.Requests.Features.Task;
using Telemart.Client.Data.Requests.Features.Task.Actions;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.TransferObjects.Task;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Store.Order;
using Telemart.Client.ViewModels.Store.Order.ProductInformation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Tasks
{
    public class TaskViewModel : TelemartEditorViewModelBase<TaskDto, TaskParameter, TaskViewItem>
    {
        private IReadOnlyDictionary<int, string> employees;
        private IReadOnlyDictionary<int, string> types;

        public TaskViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            IMessenger messenger,
            DocumentCommands documentCommands,
            ProductInformationViewModel productInformationViewModel)
            : base(webClient, dictionaries, messageFacadeService, mapper, messenger)
        {
            StartTaskCommand = new AsyncCommand(StartTaskAsync, () => Model != null && Model.EmployeeLockId == null && Model.State == TaskState.New);
            CompleteTaskCommand = new AsyncCommand(CompleteTaskAsync, () => Model != null && Model.EmployeeLockId == null && (Model.State == TaskState.New || Model.State == TaskState.InProgress));
            CancelTaskCommand = new AsyncCommand(CancelTaskAsync, () => Model != null && Model.EmployeeLockId == null && (Model.State == TaskState.New || Model.State == TaskState.InProgress));
            SuspendTaskCommand = new AsyncCommand(SuspendTaskAsync, () => Model != null && Model.EmployeeLockId == null && Model.State == TaskState.InProgress);
            ReopenTaskCommand = new AsyncCommand(ReopenTaskAsync, () => Model != null && Model.EmployeeLockId == null && (Model.State == TaskState.Completed || Model.State == TaskState.Cancelled));
            OpenOrderCommand = new DelegateCommand<int?>(OpenOrder, x => x.HasValue);
            ProcessPickupProductsCommand = new AsyncCommand(ProcessPickupProductsAsync);

            DocumentCommands = documentCommands;
            ProductInformation = productInformationViewModel;
        }

        public TaskViewModel()
        {
        }

        #region Commands

        public IAsyncCommand StartTaskCommand { get; }

        public IAsyncCommand CompleteTaskCommand { get; }

        public IAsyncCommand CancelTaskCommand { get; }

        public IAsyncCommand SuspendTaskCommand { get; }

        public IAsyncCommand ReopenTaskCommand { get; }

        public IAsyncCommand ProcessPickupProductsCommand { get; }

        public DocumentCommands DocumentCommands { get; }

        public IDelegateCommand OpenOrderCommand { get; }

        #endregion

        #region INPC

        public IEnumerable<SummaryViewItem> SummaryItems
        {
            get { return GetProperty(() => SummaryItems); }
            private set { SetProperty(() => SummaryItems, value); }
        }

        public bool ProductInfoVisible
        {
            get { return GetProperty(() => ProductInfoVisible); }
            private set { SetProperty(() => ProductInfoVisible, value); }
        }

        public int? InvoiceId
        {
            get { return GetProperty(() => InvoiceId); }
            set { SetProperty(() => InvoiceId, value); }
        }

        public int? OrderId
        {
            get { return GetProperty(() => OrderId); }
            set { SetProperty(() => OrderId, value, () => RaisePropertyChanged(nameof(OrderVisible))); }
        }

        public bool OrderVisible => OrderId.HasValue;

        public ProductInformationViewModel ProductInformation { get; }

        #endregion

        public bool CanCreateReturnInvoice =>
            Model != null
            && ProductInformation.ProductId != null
            && Model.TypeId == TaskType.ProductResolution.Id
            && Model.State == TaskState.InProgress
            && InvoiceId.HasValue;

        public bool CanProcessPickupProducts => Model != null && Model.TypeId == TaskType.ProductPickupResolutionId && Model.State == TaskState.InProgress;

        protected override string CreatedActionMessage => "создана";

        protected override string EntityName => "Задача";

        protected override string UpdatedActionMessage => "сохранена";

        protected override void OnInitializeInDesignModeInternal()
        {
            ProductInfoVisible = true;
        }

        protected override Task<Result<TaskDto>> CreateEntityAsync()
        {
            throw new NotSupportedException();
        }

        protected override object CreateEntityMessage(TaskDto taskDto, MessageType messageType)
        {
            return new TaskMessage(taskDto, messageType);
        }

        protected override Task<TaskDto> GetEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new QueryTask(id));
        }

        protected override async Task HandleLoadedAsync()
        {
            await Task.WhenAll(RefreshEmployeesAsync(), RefreshTypesAsync());

            await base.HandleLoadedAsync();
        }

        protected override void AfterSetData()
        {
            RefreshSummaryItems();

            if (Model.TypeId == TaskType.ProductResolution.Id
                && Model.Documents.TryGetValue("id_product", out int productId)
                && Model.Documents.TryGetValue("id_currency", out int currencyId)
                && Model.Documents.TryGetValue("id_invoice", out int invoiceId))
            {
                InvoiceId = invoiceId;
                ProductInformation.ProductId = new ProductInfoId(productId, currencyId);
                ProductInfoVisible = true;
            }
            else
            {
                OrderId = (Model.TypeId == TaskType.UnpackAndCancelOrder.Id || Model.TypeId == TaskType.CancelOrder.Id)
                          && Model.Documents.TryGetValue("id_order", out int orderId)
                    ? orderId
                    : null;

                ProductInformation.ClearProduct();
                ProductInfoVisible = false;
            }

            RaisePropertiesChanged(nameof(CanCreateReturnInvoice), nameof(CanProcessPickupProducts), nameof(OrderVisible));
        }

        protected override IEnumerable<string> GetMembersToIgnore()
        {
            yield return nameof(Model.Documents);
        }

        protected override Task<LockResponse<TaskDto>> LockEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new LockTask(id, checkPermissions: true));
        }

        protected override Task<LockResponse<TaskDto>> UnlockEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new UnlockTask(id));
        }

        protected override void SetCreateTitle()
        {
            throw new NotSupportedException();
        }

        protected override void SetEditTitle()
        {
            Title = $"Задача №{Model.Id}";
        }

        protected override Task<Result<TaskDto>> UpdateEntityAsync()
        {
            throw new NotSupportedException();
        }

        private async Task ProcessPickupProductsAsync()
        {
            await ExecuteLockableOperationAsync(lockedTask =>
            {
                PickupProductsDto productsDto = lockedTask.Documents.ToObject<PickupProductsDto>();

                if (productsDto is null)
                {
                    MessageFacadeService.ShowNotificationError("Не удалось преобразовать список товаров");
                    return;
                }

                ProcessPickupProductsViewModel viewModel = DialogDocumentManagerService.ShowView<ProcessPickupProductsViewModel>(new ProcessPickupProductsParameter(productsDto.Products, Model.Id), this);

                if (!viewModel.IsOk)
                {
                    return;
                }

                Model.Documents = viewModel.TaskDto.Documents;
            });
        }

        private async Task RefreshEmployeesAsync()
        {
            PagedResult<EmployeeDto> pagedResult = await WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true);

            employees = pagedResult.Data.ToDictionary(x => x.Id, y => y.Name);
        }

        private async Task RefreshTypesAsync()
        {
            List<TaskTypeDto> taskTypes = await WebClient.ExecuteApiRequestAsync(new QueryTaskTypes(), true);

            types = taskTypes.ToDictionary(x => x.Id, y => y.Name);
        }

        private void RefreshSummaryItems()
        {
            SummaryItems = GetSummaryItems();
        }

        private IEnumerable<SummaryViewItem> GetSummaryItems()
        {
            const string Format = DateFormattingRules.FullDateTimeFormat;

            yield return new SummaryViewItem("Номер", Model.Id.ToString());
            yield return new SummaryViewItem("Тип", types.GetValueOrDefault(Model.TypeId));

            if (Model.EmployeeId.HasValue)
            {
                yield return new SummaryViewItem("Ответственный", employees.GetValueOrDefault(Model.EmployeeId.Value));
            }

            yield return new SummaryViewItem("Дедлайн", Model.Deadline.ToString(Format));
            yield return new SummaryViewItem("Статус", Model.State.Name);
            yield return new SummaryViewItem("Создал", $"{employees.GetValueOrDefault(Model.CreatedBy)} ({Model.CreatedOn.ToString(Format)})");
            yield return new SummaryViewItem("Изменил", $"{employees.GetValueOrDefault(Model.ModifiedBy)} ({Model.ModifiedOn.ToString(Format)})");

            if (Model.CompletedBy.HasValue)
            {
                yield return new SummaryViewItem("Завершил", $"{employees.GetValueOrDefault(Model.CompletedBy.Value)} ({Model.CompletedOn?.ToString(Format)})");
            }
        }

        private Task StartTaskAsync()
        {
            return ExecuteLockableOperationAsync(x => WebClient.ExecuteApiRequestAsync(new StartTask(x.Id)));
        }

        private Task CompleteTaskAsync()
        {
            return ExecuteLockableOperationAsync(x => WebClient.ExecuteApiRequestAsync(new CompleteTask(x.Id)));
        }

        private Task CancelTaskAsync()
        {
            return ExecuteLockableOperationAsync(x => WebClient.ExecuteApiRequestAsync(new CancelTask(x.Id)));
        }

        private Task SuspendTaskAsync()
        {
            return ExecuteLockableOperationAsync(x => WebClient.ExecuteApiRequestAsync(new SuspendTask(x.Id)));
        }

        private Task ReopenTaskAsync()
        {
            return ExecuteLockableOperationAsync(x => WebClient.ExecuteApiRequestAsync(new ReopenTask(x.Id)));
        }

        private void OpenOrder(int? orderId)
        {
            Messenger.Send(new OrderEditViewMessage(OrderId!.Value));
        }
    }
}