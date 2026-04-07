using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.Carry;
using Telemart.Client.Data.Requests.Features.Movement;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Warehouse;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Warehouse.Movement
{
    public sealed class CreateMovementViewModel : TelemartDialogViewModelBase
    {
        private IReadOnlyDictionary<int, List<MovementRouteScheduleDto>> movementRouteSchedules;
        private Dictionary<int, WarehouseViewItem> warehousesDictionary;

        public CreateMovementViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Mapper = mapper;
            HandleFromWarehouseChangedCommand = new AsyncCommand(HandleFromWarehouseChangedAsync);
            HandleToWarehouseChangedCommand = new DelegateCommand(HandleToWarehouseChanged);

            CalculatePurposesEnabled();
        }

        public CreateMovementViewModel()
        {
        }

        #region Commands

        public IAsyncCommand HandleFromWarehouseChangedCommand { get; }

        public DelegateCommand HandleToWarehouseChangedCommand { get; }

        #endregion

        #region INPC

        public WarehouseViewItem FromWarehouse
        {
            get { return GetProperty(() => FromWarehouse); }
            set { SetProperty(() => FromWarehouse, value, () => RaisePropertiesChanged(nameof(AllowChangeMovementDate))); }
        }

        public ObservableCollection<WarehouseViewItem> FromWarehouses
        {
            get { return GetProperty(() => FromWarehouses); }
            private set { SetProperty(() => FromWarehouses, value); }
        }

        public MovementRouteScheduleDto RouteTime
        {
            get { return GetProperty(() => RouteTime); }
            set { SetProperty(() => RouteTime, value, OnRouteTimeChanged); }
        }

        public List<object> SelectedPurposes
        {
            get { return GetProperty(() => SelectedPurposes); }
            set { SetProperty(() => SelectedPurposes, value); }
        }

        public int? CarryTypeId
        {
            get { return GetProperty(() => CarryTypeId); }
            set { SetProperty(() => CarryTypeId, value, () => RaisePropertyChanged(nameof(DeliveryType))); }
        }

        public DeliveryTypeDto DeliveryType
        {
            get { return GetProperty(() => DeliveryType); }
            set { SetProperty(() => DeliveryType, value); }
        }

        public DateTime? DateOut
        {
            get { return GetProperty(() => DateOut); }
            set { SetProperty(() => DateOut, value, () => RaisePropertyChanged(nameof(DateDeparture))); }
        }

        public DateTime? DateDeparture
        {
            get { return GetProperty(() => DateDeparture); }
            set { SetProperty(() => DateDeparture, value, () => RaisePropertyChanged(nameof(DateArrive))); }
        }

        public DateTime? DateArrive
        {
            get { return GetProperty(() => DateArrive); }
            set { SetProperty(() => DateArrive, value, () => RaisePropertyChanged(nameof(DateIn))); }
        }

        public DateTime? DateIn
        {
            get { return GetProperty(() => DateIn); }
            set { SetProperty(() => DateIn, value); }
        }

        public bool CreateEmpty
        {
            get { return GetProperty(() => CreateEmpty); }
            set { SetProperty(() => CreateEmpty, value, OnCreateEmptyChanged); }
        }

        public ReadOnlyObservableCollection<MovementRouteScheduleDto> RouteTimes
        {
            get { return GetProperty(() => RouteTimes); }
            private set { SetProperty(() => RouteTimes, value); }
        }

        public int? ToWarehouseId
        {
            get { return GetProperty(() => ToWarehouseId); }
            set { SetProperty(() => ToWarehouseId, value); }
        }

        public ObservableCollection<ComboBoxItem> ToWarehouses
        {
            get { return GetProperty(() => ToWarehouses); }
            private set { SetProperty(() => ToWarehouses, value); }
        }

        public bool MoveFreeStocks
        {
            get { return GetProperty(() => MoveFreeStocks); }
            set { SetProperty(() => MoveFreeStocks, value, MoveFreeStocksChanged); }
        }

        public bool PurposesEnabled
        {
            get { return GetProperty(() => PurposesEnabled); }
            private set { SetProperty(() => PurposesEnabled, value); }
        }

        public bool MoveFreeStocksVisible
        {
            get { return GetProperty(() => MoveFreeStocksVisible); }
            private set { SetProperty(() => MoveFreeStocksVisible, value); }
        }

        public ReadOnlyObservableCollection<CarryType> CarryTypes
        {
            get { return GetProperty(() => CarryTypes); }
            private set { SetProperty(() => CarryTypes, value); }
        }

        public ReadOnlyObservableCollection<DeliveryTypeDto> DeliveryTypes
        {
            get { return GetProperty(() => DeliveryTypes); }
            private set { SetProperty(() => DeliveryTypes, value); }
        }

        public ReadOnlyObservableCollection<WarehouseRouteTimePurpose> Purposes
        {
            get { return GetProperty(() => Purposes); }
            private set { SetProperty(() => Purposes, value); }
        }

        public bool AllowChangeMovementDate => WebClient.IsOperationAllowed(BusinessOperation.MovementCustomCreate)
                && (CreateEmpty || (FromWarehouse != null && WebClient.AuthenticatedEmployee.AllowWarehouses.Contains(FromWarehouse.Id)));

        #endregion

        public MovementDto CreatedMovement { get; private set; }

        private IMapper Mapper { get; }

        public static void BuildMetadata(MetadataBuilder<CreateMovementViewModel> builder)
        {
            builder.Property(x => x.SelectedPurposes).MatchesInstanceRule((x, y) => x?.Any() == true || y.CreateEmpty || y.MoveFreeStocks, () => Resources.RequiredErrorMessage);
            builder.Property(x => x.FromWarehouse).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.ToWarehouseId).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.DateOut).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.DateDeparture).Required(() => Resources.RequiredErrorMessage)
                .MatchesInstanceRule((x, y) => x > y.DateOut, () => "Отправление должно быть позже создания");
            builder.Property(x => x.DateArrive).Required(() => Resources.RequiredErrorMessage)
                .MatchesInstanceRule((x, y) => x > y.DateDeparture, () => "Прибытие должно быть позже отправления");
            builder.Property(x => x.DateIn).Required(() => Resources.RequiredErrorMessage)
                .MatchesInstanceRule((x, y) => x > y.DateArrive, () => "Оприходование должно быть позже прибытия");
            builder.Property(x => x.DeliveryType)
                .MatchesInstanceRule((x, y) => (y.CarryTypeId != CarryType.NpDeliveryId && y.CarryTypeId != CarryType.NpWarehouseId && y.CarryTypeId != CarryType.TeksId) || x != null, () => Resources.RequiredErrorMessage)
                .MatchesInstanceRule((x, y) => (y.CarryTypeId != CarryType.NpDeliveryId && y.CarryTypeId != CarryType.NpWarehouseId) || x == null || x.CarryId == y.CarryTypeId, () => "Тип доставки не соответствует выбранному способу")
                .MatchesInstanceRule((x, y) => y.CarryTypeId != CarryType.TeksId || x.Id == (int)Telemart.Client.Dictionaries.DeliveryType.DoorDoor, () => "Тип доставки должен быть \"Двери-Двери\"");
        }

        protected override async Task HandleLoadedAsync()
        {
            List<WarehouseDto> warehouses = await WebClient.ExecuteApiRequestAsync(new QueryWarehouses(), true).GetPagedResultDataAsync();

            warehousesDictionary = warehouses.ToDictionary(x => x.Id, y => Mapper.Map<WarehouseViewItem>(y));

            FromWarehouses = warehousesDictionary.Values
                .Where(x => x.Active == 1)
                .OrderByDescending(x => x.Position)
                .ThenBy(x => x.Name)
                .ToObservableCollection();

            CarryTypes = Dictionaries.GetItems<CarryType>().Where(x => x.UseInMovements).ToReadOnlyObservableCollection();
            Purposes = Dictionaries.GetItems<WarehouseRouteTimePurpose>().ToReadOnlyObservableCollection();

            List<DeliveryTypeDto> deliveryTypes = await WebClient.ExecuteApiRequestAsync(new QueryDeliveryTypes());

            DeliveryTypes = deliveryTypes.ToReadOnlyObservableCollection();

            CalculatePurposesEnabled();

            Title = "Создание перемещения";
        }

        protected override async Task HandleOkAsync()
        {
            if (IDataErrorInfoHelper.HasErrors(this, 1))
            {
                return;
            }

            if (CreateEmpty && RouteTimes?.Any(x => x.DateIn == DateIn && x.DateOut == DateOut && x.DateArrive == DateArrive && x.DateDeparture == DateDeparture) == true)
            {
                MessageFacadeService.ShowNotificationError("Нельзя создавать пустые перемещения для перемещений, которые есть в графике");
                return;
            }

            if (DateOut == null || DateDeparture == null || DateArrive == null || DateIn == null)
            {
                MessageFacadeService.ShowNotificationWarning("Выберите даты перемещения");
                return;
            }

            if (DateOut > DateTime.Now)
            {
                MessageFacadeService.ShowNotificationWarning("Время отправки перемещения еще не пришло");
                return;
            }

            if (DateIn <= DateTime.Now && !MessageFacadeService.Confirm("Время поступления перемещения уже прошло, продолжить?"))
            {
                return;
            }

            try
            {
                CreateMovement gatewayRequest = new CreateMovement(
                    FromWarehouse.Id,
                    ToWarehouseId!.Value,
                    DateOut.Value,
                    DateDeparture.Value,
                    DateArrive.Value,
                    DateIn.Value,
                    MoveFreeStocksVisible && MoveFreeStocks,
                    CreateEmpty,
                    CarryTypeId,
                    DeliveryType?.Id,
                    SelectedPurposes?.Cast<WarehouseRouteTimePurpose>().Select(x => x.Id).ToArray());

                Result<MovementDto> result = await WebClient.ExecuteApiRequestAsync(gatewayRequest);

                MessageFacadeService.ShowNotificationInfo($"Перемещение №{result.Data.Id} успешно создано");

                CreatedMovement = result.Data;

                IsOk = true;
                Close();
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при создании перемещения");
                ShowValidationResultView("Ошибки при создании перемещения", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to create movement");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to create movement");
                MessageFacadeService.ShowNotificationError("Ошибка при создании перемещения");
            }
        }

        private async Task HandleFromWarehouseChangedAsync()
        {
            ToWarehouseId = null;
            RouteTime = null;
            ToWarehouses = null;
            RouteTimes = null;

            MoveFreeStocks = false;
            MoveFreeStocksVisible = false;

            if (FromWarehouse == null)
            {
                return;
            }

            try
            {
                MoveFreeStocksVisible = FromWarehouse.TypeId == WarehouseKind.Pickup.Id || FromWarehouse.TypeId == WarehouseKind.ShowCase.Id || FromWarehouse.TypeId == WarehouseKind.Assembly.Id;

                List<MovementRouteDto> movementRoutes = await WebClient.ExecuteApiRequestAsync(new QueryWarehouseMovementRoutes(FromWarehouse.Id));

                movementRouteSchedules = movementRoutes.ToDictionary(x => x.WarehouseToId, x => x.Schedules);

                HashSet<int> warehouseToIds = movementRoutes.Select(x => x.WarehouseToId).ToHashSet();

                ToWarehouses = warehousesDictionary.Values
                    .Where(x => warehouseToIds.Contains(x.Id) && x.IsActive)
                    .Select(x => new ComboBoxItem(x.Id, x.Name)).ToObservableCollection();
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при получении графиков");
                ShowValidationResultView("Ошибки при получении графиков", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to get warehouse movement targets");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to get warehouse movement targets");
                MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
            }
        }

        private void HandleToWarehouseChanged()
        {
            RouteTime = null;
            RouteTimes = null;

            if (FromWarehouse == null || ToWarehouseId == null)
            {
                return;
            }

            List<MovementRouteScheduleDto> routeSchedules = movementRouteSchedules[ToWarehouseId.Value];

            if (routeSchedules.Any())
            {
                RouteTimes = routeSchedules.ToReadOnlyObservableCollection();
            }
            else
            {
                MessageFacadeService.ShowNotificationWarning("В данный момент перемещение не доступно");
            }
        }

        private void OnCreateEmptyChanged()
        {
            if (CreateEmpty)
            {
                DateArrive = null;
                DateDeparture = null;
                DateIn = null;
                DateOut = null;
                CarryTypeId = null;
                DeliveryType = null;
            }

            CalculatePurposesEnabled();

            RaisePropertiesChanged(nameof(AllowChangeMovementDate), nameof(SelectedPurposes));
        }

        private void OnRouteTimeChanged()
        {
            if (RouteTime != null)
            {
                SelectedPurposes = CreateEmpty ? null : RouteTime.PurposeIds
                    .Select(x => Purposes.First(z => z.Id == x)).Cast<object>()
                    .ToList();
                DateOut = RouteTime.DateOut;
                DateDeparture = RouteTime.DateDeparture;
                DateArrive = RouteTime.DateArrive;
                DateIn = RouteTime.DateIn;
                CarryTypeId = RouteTime.CarryId;
                DeliveryType = DeliveryTypes.FirstOrDefault(x => x.Id == RouteTime.DeliveryTypeId);
            }
            else
            {
                DateOut = null;
                DateDeparture = null;
                DateArrive = null;
                DateIn = null;
                CarryTypeId = null;
                DeliveryType = null;
                SelectedPurposes = null;
            }
        }

        private void CalculatePurposesEnabled()
        {
            PurposesEnabled = WebClient.IsOperationAllowed(BusinessOperation.WarehouseRouteEditPurpose) && !CreateEmpty;

            if (!PurposesEnabled && CreateEmpty)
            {
                SelectedPurposes = null;
            }
        }

        private void MoveFreeStocksChanged()
        {
            RaisePropertyChanged(nameof(SelectedPurposes));
        }
    }
}