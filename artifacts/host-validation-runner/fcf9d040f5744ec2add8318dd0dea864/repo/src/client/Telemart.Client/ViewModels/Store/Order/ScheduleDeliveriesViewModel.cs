using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Grid;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Carry;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.Requests.Features.Logistics;
using Telemart.Client.Data.Requests.Features.Order.Actions;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Helpers;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Carry;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.ViewModels.Base;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Store.Order
{
    public sealed class ScheduleDeliveriesViewModel : TelemartDialogViewModelBase
    {
        private readonly IErrorHandler _errorHandler;
        private TelemartEnumerableCompareHelper<ScheduleDeliveryViewItem> _compareHelper;

        public ScheduleDeliveriesViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IErrorHandler errorHandler)
            : base(webClient, dictionaries, messageFacadeService)
        {
            _errorHandler = errorHandler;

            RowDoubleClickCommand = new DelegateCommand<RowDoubleClickEventArgs>(RowDoubleClick, x => SelectedScheduleDeliveryViewItem != null && x != null);
        }

        public IDelegateCommand RowDoubleClickCommand { get; set; }

        public int? SelectedCourierEmployeeId
        {
            get { return GetProperty(() => SelectedCourierEmployeeId); }
            set { SetProperty(() => SelectedCourierEmployeeId, value, SelectedCourierEmployeeIdChanged); }
        }

        public CourierDeliveryViewItem SelectedCourierDelivery
        {
            get { return GetProperty(() => SelectedCourierDelivery); }
            set { SetProperty(() => SelectedCourierDelivery, value, CourierDeliveryChanged); }
        }

        public ReadOnlyObservableCollection<CourierDeliveryViewItem> CourierDeliveries
        {
            get { return GetProperty(() => CourierDeliveries); }
            private set { SetProperty(() => CourierDeliveries, value); }
        }

        public ReadOnlyObservableCollection<ScheduleDeliveryViewItem> Orders
        {
            get { return GetProperty(() => Orders); }
            private set { SetProperty(() => Orders, value); }
        }

        public ScheduleDeliveryViewItem SelectedScheduleDeliveryViewItem
        {
            get { return GetProperty(() => SelectedScheduleDeliveryViewItem); }
            set { SetProperty(() => SelectedScheduleDeliveryViewItem, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> CourierEmployees
        {
            get { return GetProperty(() => CourierEmployees); }
            private set { SetProperty(() => CourierEmployees, value); }
        }

        protected override async Task HandleLoadedAsync()
        {
            ScheduleDeliveriesParameter parameter = (ScheduleDeliveriesParameter)Parameter;

            Task<PagedResult<EmployeeDto>> employeesTask = WebClient.ExecuteApiRequestAsync(new QueryEmployees(null, null, true, null), true);

            Task<List<CourierDeliveryDto>> courierDeliveriesTask = WebClient.ExecuteApiRequestAsync(new QueryCourierDeliveries(), true);

            Task<List<DeliveryServicePlaceDto>> placeTask = WebClient.ExecuteApiRequestAsync(new QueryCarryPlaces(parameter.CarryId, parameter.CityId));

            await Task.WhenAll(employeesTask, courierDeliveriesTask, placeTask);

            IReadOnlyDictionary<string, string> places = placeTask.Result.ToDictionary(x => x.PlaceId, x => x.PlaceName);

            CarryDto carry = await WebClient.ExecuteApiRequestAsync(new QueryCarry(parameter.CarryId));

            CourierDeliveries = courierDeliveriesTask.Result
                .Where(x => x.CarryId == parameter.CarryId && DayOfWeekHelper.Parse(x.DaysOfWeek).Contains(parameter.Date.DayOfWeek))
                .Select(x => new CourierDeliveryViewItem(x.TimeDeliveryFrom, x.TimeDeliveryTo))
                .ToReadOnlyObservableCollection();

            Orders = parameter.Orders
                .Where(x => x.DeliveryTime.HasValue && DateOnly.FromDateTime(x.DeliveryTime.Value.Date) == parameter.Date)
                .Select(x => new ScheduleDeliveryViewItem()
                {
                    OrderId = x.Id,
                    SelectedCourierEmployeeId = x.CourierEmployeeId,
                    CustomerPhone = x.Phone,
                    DateX = x.DeliveryTime!,
                    DeliveryTime = x.DeliveryTime!.Value.TimeOfDay,
                    DeliveryTimeTo = x.DeliveryTimeTo!.Value.TimeOfDay,
                    DeliveryDateOld = DateOnly.FromDateTime(x.DeliveryTime.Value),
                    SelectedCourierDelivery = GetCourierDelivery(x.DeliveryTime!.Value.TimeOfDay, x.DeliveryTimeTo!.Value.TimeOfDay),
                    Address = AddressHelper.GetFullAddress(
                        x.DeliveryData.House,
                        x.DeliveryData.Flat,
                        x.DeliveryData.Extra,
                        places.GetValueOrDefault(x.DeliveryData.PlaceId))
                })
                .ToReadOnlyObservableCollection();

            if (!Orders.Any())
            {
                MessageFacadeService.ShowNotificationWarning("Нет заказов для планирования");
                Close();
                return;
            }

            CourierEmployees = employeesTask.Result.Data
                .Where(x => carry.Drivers.Contains(x.Id))
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .OrderBy(x => x.DisplayValue)
                .ToReadOnlyObservableCollection();

            if (!CourierDeliveries.Any())
            {
                MessageFacadeService.ShowNotificationWarning("На выбранный день отсутствует график");
                Close();
                return;
            }

            _compareHelper = new TelemartEnumerableCompareHelper<ScheduleDeliveryViewItem>(Orders);

            await base.HandleLoadedAsync();

            Title = "Массовое планирование доставки заказов";
        }

        protected override async Task HandleOkAsync()
        {
            if (!_compareHelper.IsChanged())
            {
                MessageFacadeService.ShowNotificationWarning("нечего изменять");
                return;
            }

            ScheduleDeliveryDto dto = new ScheduleDeliveryDto()
            {
                Orders = Orders
                    .Select(
                        x =>
                            new ScheduleDeliveryOrderDto()
                            {
                                OrderId = x.OrderId,
                                CourierEmployeeId = x.SelectedCourierEmployeeId!.Value,
                                DeliveryTime = x.DeliveryDateNew == x.DeliveryDateOld
                                    ? x.DeliveryDateOld.ToDateTime(TimeOnly.FromTimeSpan(x.DeliveryTime))
                                    : x.DeliveryDateNew.ToDateTime(TimeOnly.FromTimeSpan(x.DeliveryTime)),
                                DeliveryTimeTo = x.DeliveryDateNew == x.DeliveryDateOld
                                    ? x.DeliveryDateOld.ToDateTime(TimeOnly.FromTimeSpan(x.DeliveryTimeTo))
                                    : x.DeliveryDateNew.ToDateTime(TimeOnly.FromTimeSpan(x.DeliveryTimeTo)),
                                Comment = $"{x.Comment}{x.CommentChangeDataX}"
                            })
                    .ToArray()
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

        private void SelectedCourierEmployeeIdChanged()
        {
            if (SelectedCourierEmployeeId is null)
            {
                return;
            }

            foreach (ScheduleDeliveryViewItem order in Orders)
            {
                order.SelectedCourierEmployeeId = SelectedCourierEmployeeId;
            }
        }

        private void CourierDeliveryChanged()
        {
            if (SelectedCourierDelivery is null)
            {
                return;
            }

            foreach (ScheduleDeliveryViewItem order in Orders)
            {
                order.SelectedCourierDelivery = SelectedCourierDelivery;
            }
        }

        private void RowDoubleClick(RowDoubleClickEventArgs args)
        {
            ScheduleDeliveryViewItem item = (ScheduleDeliveryViewItem)((GridControl)args.Source.DataControl).CurrentItem;

            if (args.HitInfo.Column.FieldName == nameof(ScheduleDeliveryViewItem.Address))
            {
                Clipboard.Clear();
                Clipboard.SetText(item.Address);
            }
        }

        private CourierDeliveryViewItem GetCourierDelivery(TimeSpan deliveryTime, TimeSpan deliveryTimeTo)
        {
            return CourierDeliveries.FirstOrDefault(x => x.TimeDeliveryFrom == deliveryTime && x.TimeDeliveryTo == deliveryTimeTo);
        }
    }
}