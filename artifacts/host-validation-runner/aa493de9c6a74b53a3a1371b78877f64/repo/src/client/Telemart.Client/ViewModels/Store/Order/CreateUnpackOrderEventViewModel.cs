using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Newtonsoft.Json.Linq;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.Requests.Features.Event;
using Telemart.Client.Data.Requests.Features.Order;
using Telemart.Client.Data.Requests.Features.Task;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.TransferObjects.Task;
using Telemart.Client.TransferObjects.Warehouse;
using Telemart.Client.ViewModels.Base;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Store.Order
{
    public sealed class CreateUnpackOrderEventViewModel : TelemartDialogViewModelBase
    {
        private readonly IErrorHandler _errorHandler;

        public CreateUnpackOrderEventViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IErrorHandler errorHandler)
            : base(webClient, dictionaries, messageFacadeService)
        {
            _errorHandler = errorHandler;
        }

        public int OrderId
        {
            get { return GetProperty(() => OrderId); }
            set { SetProperty(() => OrderId, value); }
        }

        public int WarehouseId
        {
            get { return GetProperty(() => WarehouseId); }
            set { SetProperty(() => WarehouseId, value); }
        }

        public string Comment
        {
            get { return GetProperty(() => Comment); }
            set { SetProperty(() => Comment, value); }
        }

        public bool AllowSelectChangeReason
        {
            get { return GetProperty(() => AllowSelectChangeReason); }
            private set { SetProperty(() => AllowSelectChangeReason, value); }
        }

        public ObservableCollection<OrderStateChangeReasonViewItem> ChangeReasons
        {
            get { return GetProperty(() => ChangeReasons); }
            private set { SetProperty(() => ChangeReasons, value); }
        }

        public ReadOnlyObservableCollection<EventType> UnpackOrderEventTypes
        {
            get { return GetProperty(() => UnpackOrderEventTypes); }
            private set { SetProperty(() => UnpackOrderEventTypes, value); }
        }

        public OrderStateChangeReasonViewItem SelectedChangeReason
        {
            get { return GetProperty(() => SelectedChangeReason); }
            set { SetProperty(() => SelectedChangeReason, value); }
        }

        public bool EventForWarehouseEmployees
        {
            get { return GetProperty(() => EventForWarehouseEmployees); }
            private set { SetProperty(() => EventForWarehouseEmployees, value); }
        }

        public bool TaskForPickupEmployees
        {
            get { return GetProperty(() => TaskForPickupEmployees); }
            set { SetProperty(() => TaskForPickupEmployees, value); }
        }

        public EventType SelectedUnpackOrderEventType
        {
            get { return GetProperty(() => SelectedUnpackOrderEventType); }
            set { SetProperty(() => SelectedUnpackOrderEventType, value, SelectedUnpackOrderEventTypeChanged); }
        }

        public static void BuildMetadata(MetadataBuilder<CreateUnpackOrderEventViewModel> builder)
        {
            builder.Property(x => x.SelectedUnpackOrderEventType)
                .Required(() => Resources.RequiredErrorMessage);
        }

        protected override async Task HandleLoadedAsync()
        {
            CreateUnpackOrderEventParameter parameter = (CreateUnpackOrderEventParameter)Parameter;

            OrderId = parameter.OrderId;
            WarehouseId = parameter.WarehouseId;
            EventForWarehouseEmployees = parameter.EventForWarehouseEmployees;
            TaskForPickupEmployees = parameter.TaskForPickupEmployees;

            List<OrderStateChangeReasonDto> reasons = await WebClient.ExecuteApiRequestAsync(new QueryOrderStateChangeReasons(false));

            ChangeReasons = reasons
                .Where(x => x.StateId == null || x.StateId == OrderStatus.Canceled.Id)
                .Select(x => new OrderStateChangeReasonViewItem(x.Id, x.ParentId ?? 0, x.Name, x.Position))
                .ToObservableCollection();

            UnpackOrderEventTypes = new[]
            {
                EventType.UnpackOrder,
                EventType.UnpackAndCancelOrder
            }.ToReadOnlyObservableCollection();

            await base.HandleLoadedAsync();

            Title = $"Инициирование распаковки по заказу №{OrderId}";
        }

        protected override async Task HandleOkAsync()
        {
            if (SelectedChangeReason == null && SelectedUnpackOrderEventType?.Id == EventType.UnpackAndCancelOrderId)
            {
                MessageFacadeService.ShowNotificationWarning("Выберите причину отмены");
                return;
            }

            if (ChangeReasons.Any(x => x.ParentId == SelectedChangeReason?.Id))
            {
                MessageFacadeService.ShowNotificationWarning("Выберите причину, а не группу");
                return;
            }

            JObject documents = null;

            if (SelectedChangeReason != null)
            {
                documents = new JObject(new JProperty(EventConstants.OrderStateChangeReasonId, SelectedChangeReason.Id));
            }

            if (EventForWarehouseEmployees)
            {
                EventCreateDto createDto = new EventCreateDto(
                    SelectedUnpackOrderEventType!.Id,
                    OrderId,
                    Comment,
                    documents);

                Result<EventDto> result = await _errorHandler.HandleErrorsAsync(
                    _ => WebClient.ExecuteApiRequestAsync(new CreateEvent(createDto)),
                    "создании события распаковки",
                    "Событие распаковки создано",
                    this,
                    true);

                if (result?.IsSuccess == true)
                {
                    CloseOk();
                }
            }

            if (TaskForPickupEmployees)
            {
                int taskTypeId = SelectedUnpackOrderEventType!.Id switch
                {
                    EventType.UnpackOrderId => TaskType.UnpackOrder.Id,
                    EventType.UnpackAndCancelOrderId => TaskType.UnpackAndCancelOrder.Id,
                    _ => throw new NotSupportedException($"Event type id {SelectedUnpackOrderEventType.Id} is not supported.")
                };

                documents ??= new JObject();

                documents.Add(new JProperty(EventConstants.OrderId, OrderId));

                Task<List<WarehouseAllowedEmployeesDto>> warehousesAllowedEmployeesTask = WebClient.ExecuteApiRequestAsync(new QueryWarehouseAllowedEmployees());
                Task<WarehouseDto> warehouseTask = WebClient.ExecuteApiRequestAsync(new QueryWarehouse(WarehouseId));
                Task<PagedResult<EmployeeDto>> employeesTask = WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true);

                await Task.WhenAll(warehousesAllowedEmployeesTask, warehouseTask, employeesTask);

                HashSet<int> warehouseOrSellerEmployeeIds = employeesTask.Result.Data
                    .Where(x => x.Active && (x.Roles.Contains(Role.Seller.Name) || x.Roles.Contains(Role.Warehouse.Name)))
                    .Select(x => x.Id)
                    .ToHashSet();

                int[] employeeIds = warehousesAllowedEmployeesTask.Result
                    .FirstOrDefault(x => x.WarehouseId == WarehouseId)?.EmployeeIds?
                    .Where(x => warehouseOrSellerEmployeeIds.Contains(x))
                    .ToArray();

                TaskCreateDto createDto = new TaskCreateDto()
                {
                    Task = $"{SelectedUnpackOrderEventType.Name}. По заказу {OrderId}. ({DateTime.Now.ToString("dd.MM.yy HH:mm", CultureInfo.InvariantCulture)})",
                    Description = Comment ?? string.Empty,
                    TypeId = taskTypeId,
                    TaskDeadlineType = (int)TaskDeadlineType.DayEndIfTreeHoursLeftOrTwoHoursNextDay,
                    WorkScheduleType = WorkScheduleType.ShopTypeId,
                    Documents = documents,
                    EmployeeId = warehouseTask.Result.EmployeeId,
                    EmployeeIds = employeeIds
                };

                Result<TaskDto> result = await _errorHandler.HandleErrorsAsync(
                    _ => WebClient.ExecuteApiRequestAsync(new CreateTask(createDto)),
                    "создании задачи на распаковку",
                    "Задача на распаковку создана",
                    this,
                    true);

                if (result?.IsSuccess == true)
                {
                    CloseOk();
                }
            }
        }

        private void SelectedUnpackOrderEventTypeChanged()
        {
            if (SelectedUnpackOrderEventType == EventType.UnpackAndCancelOrder)
            {
                AllowSelectChangeReason = true;
            }
            else
            {
                AllowSelectChangeReason = false;
                SelectedChangeReason = null;
            }
        }
    }
}