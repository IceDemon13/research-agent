using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Grid;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.Carry;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Warehouse;
using Telemart.Client.TransferObjects.Warehouse.Route;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Validation;
using WarehouseRouteTimeDto = Telemart.Client.TransferObjects.Warehouse.Route.WarehouseRouteTimeDto;

namespace Telemart.Client.ViewModels.Warehouse
{
    public sealed class WarehouseRouteEditViewModel : TelemartDialogViewModelBase
    {
        private readonly Stack<int> deletedNewRouteTimeIds = new Stack<int>();

        private WarehouseRouteEditParameter routeParameter;

        private int addRouteTimeNextId = -1;

        public WarehouseRouteEditViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Mapper = mapper;

            DeleteRouteTimeCommand = new DelegateCommand<WarehouseRouteTimeViewItem>(DeleteRouteTime, x => x != null);
            CreateRouteTimeCommand = new DelegateCommand<InitNewRowEventArgs>(CreateRouteTime);
        }

        public WarehouseRouteEditViewModel()
        {
        }

        public IDelegateCommand DeleteRouteTimeCommand { get; }

        public IDelegateCommand CreateRouteTimeCommand { get; }

        #region INPC

        public ObservableCollection<WarehouseRouteTimeViewItem> WarehouseRouteTimes
        {
            get { return GetProperty(() => WarehouseRouteTimes); }
            set { SetProperty(() => WarehouseRouteTimes, value); }
        }

        public ReadOnlyObservableCollection<CarryType> CarryTypes
        {
            get { return GetProperty(() => CarryTypes); }
            set { SetProperty(() => CarryTypes, value); }
        }

        public ReadOnlyObservableCollection<WarehouseRouteTimePurpose> Purposes
        {
            get { return GetProperty(() => Purposes); }
            private set { SetProperty(() => Purposes, value); }
        }

        public ReadOnlyObservableCollection<DeliveryTypeDto> DeliveryTypes
        {
            get { return GetProperty(() => DeliveryTypes); }
            set { SetProperty(() => DeliveryTypes, value); }
        }

        public WarehouseRouteTimeViewItem SelectedWarehouseRouteTime
        {
            get { return GetProperty(() => SelectedWarehouseRouteTime); }
            set { SetProperty(() => SelectedWarehouseRouteTime, value); }
        }

        public ReadOnlyObservableCollection<DayOfWeek> DaysOfWeek
        {
            get { return GetProperty(() => DaysOfWeek); }
            private set { SetProperty(() => DaysOfWeek, value); }
        }

        public int Weight
        {
            get { return GetProperty(() => Weight); }
            set { SetProperty(() => Weight, value); }
        }

        #endregion INPC

        private IMapper Mapper { get; }

        protected override async Task HandleLoadedAsync()
        {
            routeParameter = (WarehouseRouteEditParameter)Parameter;

            DaysOfWeek = Dictionaries.GetItems<DayOfWeek>().ToReadOnlyObservableCollection();

            WarehouseRouteFullDto warehouseRoute = await WebClient.ExecuteApiRequestAsync(new QueryWarehouseRoute(routeParameter.WarehouseId, routeParameter.RouteId));

            WarehouseRouteTimes = warehouseRoute.RouteTimes
                .Select(x => Mapper.Map<WarehouseRouteTimeViewItem>(x))
                .ToObservableCollection();

            Weight = warehouseRoute.Weight;

            foreach (WarehouseRouteTimeViewItem viewItem in WarehouseRouteTimes)
            {
                WarehouseRouteTimeDto source = warehouseRoute.RouteTimes.First(x => x.Id == viewItem.Id);

                viewItem.Purposes = source.PurposeIds?
                    .Select(x => Dictionaries.GetItemById<WarehouseRouteTimePurpose>(x))
                    .Cast<object>()
                    .ToList() ?? new List<object>();
            }

            if (routeParameter.RouteTimeId.HasValue)
            {
                SelectedWarehouseRouteTime = WarehouseRouteTimes.FirstOrDefault(x => x.Id == routeParameter.RouteTimeId);
            }

            CarryTypes = Dictionaries.GetItems<CarryType>().Where(x => x.UseInMovements).ToReadOnlyObservableCollection();
            Purposes = Dictionaries.GetItems<WarehouseRouteTimePurpose>().ToReadOnlyObservableCollection();

            List<WarehouseDto> warehouses = await WebClient.ExecuteApiRequestAsync(new QueryWarehouses()).GetPagedResultDataAsync();

            List<DeliveryTypeDto> deliveryTypes = await WebClient.ExecuteApiRequestAsync(new QueryDeliveryTypes());

            DeliveryTypes = deliveryTypes.ToReadOnlyObservableCollection();

            IReadOnlyDictionary<int, WarehouseDto> warehouseDictionary = warehouses.ToDictionary(x => x.Id);

            string warehouseFromName = warehouseDictionary.GetValueOrDefault(warehouseRoute.WarehouseFromId)?.Name;

            string warehouseToName = warehouseDictionary.GetValueOrDefault(warehouseRoute.WarehouseToId)?.Name;

            Title = $"Маршрут {warehouseFromName} - {warehouseToName}";
        }

        protected override bool CanOk()
        {
            if (WarehouseRouteTimes?.Any() != true)
            {
                return true;
            }

            foreach (WarehouseRouteTimeViewItem routeTime in WarehouseRouteTimes)
            {
                if (IDataErrorInfoHelper.HasErrors(routeTime))
                {
                    return false;
                }
            }

            return true;
        }

        protected override async Task HandleOkAsync()
        {
            if (WarehouseRouteTimes?.All(row => row.CarryId == null
                                                || (row.CarryId != CarryType.NpDeliveryId && row.CarryId != CarryType.NpWarehouseId && row.CarryId != CarryType.TeksId)
                                                || ((row.CarryId == CarryType.NpDeliveryId || row.CarryId == CarryType.NpWarehouseId) && row.DeliveryType.CarryId == row.CarryId)
                                                || (row.CarryId == CarryType.TeksId && row.DeliveryType.Id == (int)DeliveryType.DoorDoor)) != true)
            {
                MessageFacadeService.ShowNotificationWarning("В графиках присутствуют ошибки");
                return;
            }

            const string GeneralErrorMessage = "Ошибка при создании маршрута";

            WarehouseRouteSaveDto saveDto = new WarehouseRouteSaveDto
            {
                Id = routeParameter.RouteId,
                Weight = Weight,
                RouteTimes = WarehouseRouteTimes
                    .Select(x => Mapper.Map<WarehouseRouteTimeDto>(x))
                    .ToArray()
            };

            try
            {
                await WebClient.ExecuteApiRequestAsync(new UpdateWarehouseRoute(routeParameter.WarehouseId, routeParameter.RouteId, saveDto));

                IsOk = true;
                Close();

                MessageFacadeService.ShowNotificationInfo("Графики успешно сохранены");
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError(GeneralErrorMessage);
                ShowValidationResultView("Ошибки при создании маршрута", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to create warehouse route");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (System.Exception exception)
            {
                MessageFacadeService.ShowNotificationError(GeneralErrorMessage);
                Logger.LogError(exception, "Error while creating warehouse route");
            }
        }

        private int GetAddRouteTimeNextId()
        {
            if (deletedNewRouteTimeIds.Any())
            {
                return deletedNewRouteTimeIds.Pop();
            }
            else
            {
                return addRouteTimeNextId--;
            }
        }

        private void DeleteRouteTime(WarehouseRouteTimeViewItem routeTime)
        {
            if (routeTime.IsAdded)
            {
                deletedNewRouteTimeIds.Push(routeTime.Id);
            }

            WarehouseRouteTimes.Remove(routeTime);
        }

        private void CreateRouteTime(InitNewRowEventArgs e)
        {
            foreach (WarehouseRouteTimeViewItem routeTime in WarehouseRouteTimes.Where(x => x.Id == 0))
            {
                routeTime.Id = GetAddRouteTimeNextId();
            }
        }
    }
}