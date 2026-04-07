using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Utils;
using Telemart.Client.Data.Options;
using Telemart.Client.Data.Requests.Features.Carry;
using Telemart.Client.Data.Requests.Features.City;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Carry;
using Telemart.Client.TransferObjects.City;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.TransferObjects.Warehouse;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Common;
using Telemart.Client.ViewModels.Common.PrintBarcodeParameters;
using Telemart.Client.ViewModels.Store.Order;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Warehouse
{
    internal sealed class WarehousesViewModel : TelemartViewModelBase, ISupportHotkeys
    {
        private IReadOnlyDictionary<int, ComboBoxItem> employeesDictionary;
        private readonly TelegramBotOptions _telegramBotOptions;

        public WarehousesViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            IErrorHandler errorHandler,
            IMessenger messenger,
            TelegramBotOptions telegramBotOptions)
            : base(webClient, dictionaries, messageFacadeService)
        {
            _telegramBotOptions = telegramBotOptions;
            Messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
            Mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            ErrorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
            AddCommand = new DelegateCommand(Add);
            EditCommand = new DelegateCommand(Edit, () => SelectedWarehouse != null);
            ShowDeliveryBotQrCommand = new DelegateCommand(ShowDeliveryBotQr);
            EditNpAddressCommand = new AsyncCommand(EditNpAddressAsync, () => SelectedWarehouse != null);
            EditNpWarehouseCommand = new AsyncCommand(EditNpWarehouseAsync, () => SelectedWarehouse != null);
            RefreshCommand = new AsyncCommand(RefreshAsync);

            Messenger.Register<WarehouseMessage>(this, OnWarehouseMessage);
        }

        public WarehousesViewModel()
        {
        }

        #region Commands

        public IDelegateCommand AddCommand { get; }

        public IDelegateCommand EditCommand { get; }

        public IDelegateCommand ShowDeliveryBotQrCommand { get; }

        public IAsyncCommand EditNpAddressCommand { get; }

        public IAsyncCommand EditNpWarehouseCommand { get; }

        public IAsyncCommand RefreshCommand { get; }

        #endregion

        #region INPC

        public bool CanCreate
        {
            get { return GetProperty(() => CanCreate); }
            private set { SetProperty(() => CanCreate, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Cities
        {
            get { return GetProperty(() => Cities); }
            private set { SetProperty(() => Cities, value); }
        }

        public ReadOnlyObservableCollection<WarehouseKind> Types
        {
            get { return GetProperty(() => Types); }
            private set { SetProperty(() => Types, value); }
        }

        public ObservableRangeCollection<WarehouseViewItem> Warehouses
        {
            get { return GetProperty(() => Warehouses); }
            set { SetProperty(() => Warehouses, value); }
        }

        public WarehouseViewItem SelectedWarehouse
        {
            get { return GetProperty(() => SelectedWarehouse); }
            set { SetProperty(() => SelectedWarehouse, value); }
        }

        public bool IsColumnChooserVisible
        {
            get { return GetProperty(() => IsColumnChooserVisible); }
            set { SetProperty(() => IsColumnChooserVisible, value); }
        }

        public bool IsCurrentUserAllowCreate => WebClient.AuthenticatedEmployee.HasAnyRole(Role.Admin);

        public bool IsCurrentUserAllowEdit => WebClient.AuthenticatedEmployee.HasAnyRole(Role.Admin);

        #endregion

        private IDocumentManagerService DialogDocumentManagerService => GetService<IDocumentManagerService>("DialogDocumentManagerService", ServiceSearchMode.PreferParents);

        private IDocumentManagerService SizeableDialogDocumentManagerService => GetService<IDocumentManagerService>("SizeableDialogDocumentManagerService", ServiceSearchMode.PreferParents);

        private IMapper Mapper { get; }

        private IMessenger Messenger { get; }

        private IErrorHandler ErrorHandler { get; }

        public bool HandleHotkey(HotkeyMessage msg)
        {
            bool handled = false;

            switch (msg.HotkeyMessageType)
            {
                case HotkeyMessageType.Refresh:
                    RefreshCommand.Execute(null);
                    handled = true;
                    break;
                case HotkeyMessageType.Add:
                    if (CanCreate)
                    {
                        AddCommand.Execute(null);
                    }

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

        protected override Task HandleLoadedAsync()
        {
            CanCreate = WebClient.IsOperationAllowed(BusinessOperation.WarehouseCreate);
            Warehouses ??= new ObservableRangeCollection<WarehouseViewItem>();
            RefreshCommand.Execute(null);
            return Task.CompletedTask;
        }

        private void Add()
        {
            DialogDocumentManagerService.ShowView<WarehouseCreateViewModel>(null, this);
        }

        private void Edit()
        {
            SizeableDialogDocumentManagerService.ShowView<WarehouseViewModel>(new WarehouseEditParameter(SelectedWarehouse.Id), this);
        }

        private async Task EditNpWarehouseAsync()
        {
            List<DeliveryServicePlaceDto> places = await WebClient.ExecuteApiRequestAsync(new QueryCarryPlaces(CarryType.NpWarehouseId, SelectedWarehouse.CityId));

            List<ComboBoxItem> npWarehouses = places.Select(x => new ComboBoxItem(0, x.PlaceName, reference: x.PlaceId)).ToList();

            DeliveryServicePlaceDto place = places.FirstOrDefault(x => x.PlaceId == SelectedWarehouse.NpWarehouseRef);

            SelectItemViewModel viewModel = DialogDocumentManagerService
                .ShowView<SelectItemViewModel>(new SelectItemParameter(npWarehouses, "Выбор склада НП", "Склад", place is null ? null : new ComboBoxItem(0, place.PlaceName, reference: place.PlaceId)), this);

            if (!viewModel.IsOk)
            {
                return;
            }

            Result<WarehouseDto> result = await ErrorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new UpdateWarehouseNpWarehouse(
                    SelectedWarehouse.Id,
                    new UpdateWarehouseNpWarehouseDto(
                        SelectedWarehouse.Id,
                        viewModel.SelectedItem.Value.Ref))),
                "обновлении склада НП",
                "Склад обновлен",
                this,
                true);

            if (result?.Data is not null)
            {
                SelectedWarehouse.NpWarehouseRef = result.Data.NpWarehouseRef;
            }
        }

        private async Task EditNpAddressAsync()
        {
            DeliveryDataDto dto = new()
            {
                House = SelectedWarehouse.House,
                PlaceId = SelectedWarehouse.NpStreetRef
            };

            string addressOld = "-";

            if (SelectedWarehouse.NpStreetRef is not null && SelectedWarehouse.House is not null)
            {
                List<DeliveryServicePlaceDto> places = await WebClient.ExecuteApiRequestAsync(new QueryCarryPlaces(CarryType.NpDeliveryId, SelectedWarehouse.CityId));

                string streetName = places.FirstOrDefault(x => x.PlaceId == SelectedWarehouse.NpStreetRef)?.PlaceName;

                if (streetName is not null)
                {
                    addressOld = $"{streetName}. Дом {SelectedWarehouse.House}";
                }
            }

            SelectDeliveryDataParameter parameter = new(
                CarryType.NpDeliveryId,
                SelectedWarehouse.CityId,
                addressOld,
                dto,
                true);

            SelectDeliveryAddressViewModel viewModel = DialogDocumentManagerService.ShowView<SelectDeliveryAddressViewModel>(
                parameter,
                this);

            if (!viewModel.IsOk)
            {
                return;
            }

            Result<WarehouseDto> result = await ErrorHandler.HandleErrorsAsync(
                x => WebClient.ExecuteApiRequestAsync(new UpdateWarehouseNpAddress(
                    SelectedWarehouse.Id,
                    new UpdateWarehouseNpAddressDto(
                        SelectedWarehouse.Id,
                        viewModel.House,
                        viewModel.Place.PlaceId))),
                "обновлении адреса НП",
                "Адрес обновлен",
                this,
                true);

            if (result?.Data is null)
            {
                return;
            }

            SelectedWarehouse.NpStreetRef = result.Data.NpStreetRef;
            SelectedWarehouse.House = result.Data.House;
        }

        private async Task RefreshAsync()
        {
            try
            {
                Types = Dictionaries.GetItems<WarehouseKind>().ToReadOnlyObservableCollection();

                await Task.WhenAll(RefreshEmployeesAsync(), RefreshCitiesAsync());

                PagedResult<WarehouseDto> warehouses = await WebClient.ExecuteApiRequestAsync(new QueryWarehouses());

                Warehouses.Clear();

                Warehouses.AddRange(warehouses.Data
                    .OrderBy(x => x.TypeId)
                    .ThenBy(x => x.MaxPackageWeight)
                    .ThenBy(x => x.Name)
                    .Select(x => Map(x, new WarehouseViewItem())));
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, Resources.ErrorDuringDataLoading);
                MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
            }
        }

        private async Task RefreshEmployeesAsync()
        {
            List<EmployeeDto> employees = await WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true).GetPagedResultDataAsync();
            employeesDictionary = employees.ToDictionary(x => x.Id, y => new ComboBoxItem(y.Id, y.Name, y.Active));
        }

        private async Task RefreshCitiesAsync()
        {
            List<CityDto> cities = await WebClient.ExecuteApiRequestAsync(new QueryCities(), true).GetPagedResultDataAsync();
            Cities = cities.Select(x => new ComboBoxItem(x.Id, x.Name)).ToReadOnlyObservableCollection();
        }

        private void OnWarehouseMessage(WarehouseMessage message)
        {
            switch (message.MessageType)
            {
                case MessageType.Added:
                    Warehouses.Insert(0, Map(message.Entity, new WarehouseViewItem()));
                    break;
                case MessageType.Changed:
                    Warehouses.DoActionWithItem(x => x.Id == message.Entity.Id, viewItem => Map(message.Entity, new WarehouseViewItem()));
                    break;
            }
        }

        private WarehouseViewItem Map(WarehouseDto dto, WarehouseViewItem viewItem)
        {
            WarehouseViewItem mappedItem = Mapper.Map(dto, viewItem);

            mappedItem.Employee = employeesDictionary.GetValueOrDefault(dto.EmployeeId);

            if (dto.EmployeeAssemblyId != null)
            {
                mappedItem.EmployeeAssembly = employeesDictionary.GetValueOrDefault(dto.EmployeeAssemblyId.Value);
            }

            return mappedItem;
        }

        private void ShowDeliveryBotQr()
        {
            QrCodeParameter parameter = new QrCodeParameter($"https://t.me/{_telegramBotOptions.DeliveryBotName}?start", "Telegram бот");

            DialogDocumentManagerService.ShowView<QrCodeViewModel>(parameter, this);
        }
    }
}