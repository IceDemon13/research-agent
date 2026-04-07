using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.Requests.Features.Logistics;
using Telemart.Client.Data.Requests.Features.Order;
using Telemart.Client.Data.Requests.Features.Order.Actions;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Helpers;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.ViewModels.Base;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Store.Order
{
    public class ScheduleDeliveryViewModel : TelemartDialogViewModelBase
    {
        private readonly IErrorHandler _errorHandler;
        private OrderDto _order;

        public ScheduleDeliveryViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IErrorHandler errorHandler)
            : base(webClient, dictionaries, messageFacadeService)
        {
            _errorHandler = errorHandler;
        }

        public TimeSpan DeliveryTimeOld
        {
            get { return GetProperty(() => DeliveryTimeOld); }
            private set { SetProperty(() => DeliveryTimeOld, value); }
        }

        public TimeSpan DeliveryTimeToOld
        {
            get { return GetProperty(() => DeliveryTimeToOld); }
            private set { SetProperty(() => DeliveryTimeToOld, value); }
        }

        public string CourierDeliveryOld
        {
            get { return GetProperty(() => CourierDeliveryOld); }
            private set { SetProperty(() => CourierDeliveryOld, value); }
        }

        public ReadOnlyObservableCollection<CourierDeliveryViewItem> CourierDeliveries
        {
            get { return GetProperty(() => CourierDeliveries); }
            private set { SetProperty(() => CourierDeliveries, value); }
        }

        public ScheduleDeliveryViewItem ScheduleDelivery
        {
            get { return GetProperty(() => ScheduleDelivery); }
            set { SetProperty(() => ScheduleDelivery, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> CourierEmployees
        {
            get { return GetProperty(() => CourierEmployees); }
            private set { SetProperty(() => CourierEmployees, value); }
        }

        protected override async Task HandleLoadedAsync()
        {
            ScheduleDeliveryParameter parameter = (ScheduleDeliveryParameter)Parameter;

            Task<OrderDto> orderTask = WebClient.ExecuteApiRequestAsync(new QueryOrder(parameter.OrderId));

            Task<PagedResult<EmployeeDto>> employeesTask = WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true);

            Task<List<CourierDeliveryDto>> courierDeliveriesTask = WebClient.ExecuteApiRequestAsync(new QueryCourierDeliveries(), true);

            await Task.WhenAll(orderTask, employeesTask, courierDeliveriesTask);

            _order = orderTask.Result;

            CourierDeliveries = courierDeliveriesTask.Result
                .Where(x => x.CarryId == _order.CarryId && DayOfWeekHelper.Parse(x.DaysOfWeek).Contains(_order.DeliveryTime!.Value.DayOfWeek))
                .Select(x => new CourierDeliveryViewItem(x.TimeDeliveryFrom, x.TimeDeliveryTo))
                .ToReadOnlyObservableCollection();

            if (!CourierDeliveries.Any())
            {
                MessageFacadeService.ShowNotificationError("Нет доступных графиков доставки");
                Close();
                return;
            }

            CarryType carry = Dictionaries.GetItemById<CarryType>(_order.CarryId);

            CourierEmployees = employeesTask.Result.Data
                .Where(x => x.Active && carry.Drivers.Contains(x.Id))
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .OrderBy(x => x.DisplayValue)
                .ToReadOnlyObservableCollection();

            DeliveryTimeOld = _order.DeliveryTime!.Value.TimeOfDay;
            DeliveryTimeToOld = _order.DeliveryTimeTo!.Value.TimeOfDay;
            CourierDeliveryOld = $"{_order.DeliveryTime.Value.TimeOfDay:hh\\:mm} - {_order.DeliveryTimeTo.Value.TimeOfDay:hh\\:mm}";

            ScheduleDelivery = new ScheduleDeliveryViewItem()
            {
                SelectedCourierEmployeeId = _order.CourierEmployeeId,
                DeliveryDateOld = DateOnly.FromDateTime(_order.DeliveryTime.Value)
            };

            Title = $"Планирование доставки заказа №{_order.Id}";

            await base.HandleLoadedAsync();
        }

        protected override async Task HandleOkAsync()
        {
            ScheduleDeliveryDto dto = new ScheduleDeliveryDto()
            {
               Orders = new[]
               {
                   new ScheduleDeliveryOrderDto()
                   {
                       OrderId = _order.Id,
                       CourierEmployeeId = ScheduleDelivery.SelectedCourierEmployeeId!.Value,
                       DeliveryTime = ScheduleDelivery.DeliveryDateOld.ToDateTime(TimeOnly.FromTimeSpan(ScheduleDelivery.DeliveryTime)),
                       DeliveryTimeTo = ScheduleDelivery.DeliveryDateOld.ToDateTime(TimeOnly.FromTimeSpan(ScheduleDelivery.DeliveryTimeTo))
                   }
               }
            };

            Result<object> result = await _errorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new ScheduleDelivery(dto)),
                "при сохранении доставки",
                "Доставка сохранена",
                this,
                true,
                showNotification: true);

            if (result?.IsSuccess == true)
            {
                CloseOk();
            }
        }
    }
}