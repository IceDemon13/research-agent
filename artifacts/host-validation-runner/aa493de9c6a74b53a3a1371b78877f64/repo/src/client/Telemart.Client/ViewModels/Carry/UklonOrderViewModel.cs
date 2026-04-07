using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Order;
using Telemart.Client.Data.Requests.Features.Uklon;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Common;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Carry
{
    public sealed class UklonOrderViewModel : TelemartDialogViewModelBase, IDisposable
    {
        private readonly IErrorHandler _errorHandler;
        private readonly IMessenger _messenger;
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        private OrderDto _order;

        public UklonOrderViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IErrorHandler errorHandler,
            IMessenger messenger)
            : base(webClient, dictionaries, messageFacadeService)
        {
            _errorHandler = errorHandler;
            _messenger = messenger;

            CancelUklonOrderCommand = new AsyncCommand(CancelUklonOrderAsync, CanCancelUklonOrder);
            SearchUklonDriverCommand = new AsyncCommand(SearchUklonDriverAsync, () => CanCreateUklonOrder);
            OpenOrderCommand = new DelegateCommand(OpenOrder);
        }

        public IAsyncCommand CancelUklonOrderCommand { get; }

        public IAsyncCommand SearchUklonDriverCommand { get; }

        public IDelegateCommand OpenOrderCommand { get; }

        public bool CanCreateUklonOrder => string.IsNullOrWhiteSpace(DocumentId) || UklonDocumentStateId == UklonDocumentState.Canceled.Id;

        public string DocumentId
        {
            get { return GetProperty(() => DocumentId); }
            private set { SetProperty(() => DocumentId, value, () => RaisePropertyChanged(nameof(CanCreateUklonOrder))); }
        }

        public string Address
        {
            get { return GetProperty(() => Address); }
            set { SetProperty(() => Address, value, () => { if (AddressValid()) { SearchUklonAddressAsync(); } }); }
        }

        public int? UklonDocumentStateId
        {
            get { return GetProperty(() => UklonDocumentStateId); }
            private set { SetProperty(() => UklonDocumentStateId, value, () => RaisePropertyChanged(nameof(CanCreateUklonOrder))); }
        }

        public string DriverName
        {
            get { return GetProperty(() => DriverName); }
            private set { SetProperty(() => DriverName, value); }
        }

        public string DriverPhone
        {
            get { return GetProperty(() => DriverPhone); }
            private set { SetProperty(() => DriverPhone, value); }
        }

        public int? DriverCompletedOrders
        {
            get { return GetProperty(() => DriverCompletedOrders); }
            private set { SetProperty(() => DriverCompletedOrders, value); }
        }

        public bool DriverHasProblemsWithHearing
        {
            get { return GetProperty(() => DriverHasProblemsWithHearing); }
            private set { SetProperty(() => DriverHasProblemsWithHearing, value); }
        }

        public string CarModel
        {
            get { return GetProperty(() => CarModel); }
            private set { SetProperty(() => CarModel, value); }
        }

        public string CarLicencePlate
        {
            get { return GetProperty(() => CarLicencePlate); }
            private set { SetProperty(() => CarLicencePlate, value); }
        }

        public string CarBrand
        {
            get { return GetProperty(() => CarBrand); }
            private set { SetProperty(() => CarBrand, value); }
        }

        public string CarColor
        {
            get { return GetProperty(() => CarColor); }
            private set { SetProperty(() => CarColor, value); }
        }

        public int OrderId
        {
            get { return GetProperty(() => OrderId); }
            private set { SetProperty(() => OrderId, value); }
        }

        public ReadOnlyObservableCollection<UklonDocumentState> UklonDocumentStates
        {
            get { return GetProperty(() => UklonDocumentStates); }
            private set { SetProperty(() => UklonDocumentStates, value); }
        }

        public string ReceiverUklonAddressId
        {
            get { return GetProperty(() => ReceiverUklonAddressId); }
            private set { SetProperty(() => ReceiverUklonAddressId, value); }
        }

        public string ReceiverUklonAddressName
        {
            get { return GetProperty(() => ReceiverUklonAddressName); }
            private set { SetProperty(() => ReceiverUklonAddressName, value); }
        }

        public ObservableCollection<ComboBoxItem> UklonAddresses
        {
            get { return GetProperty(() => UklonAddresses); }
            private set { SetProperty(() => UklonAddresses, value); }
        }

        public ComboBoxItem SelectedUklonAddress
        {
            get { return GetProperty(() => SelectedUklonAddress); }
            set { SetProperty(() => SelectedUklonAddress, value, () => { ReceiverUklonAddressId = SelectedUklonAddress.Ref; ReceiverUklonAddressName = SelectedUklonAddress.DisplayValue; }); }
        }

        public bool AddressValid()
        {
             return !string.IsNullOrWhiteSpace(Address) && Address.Length >= 3;
        }

        public static void BuildMetadata(MetadataBuilder<UklonOrderViewModel> builder)
        {
            builder.Property(x => x.Address)
                .MatchesInstanceRule((_, x) => x.AddressValid(), () => Resources.RequiredErrorMessage);
            builder.Property(x => x.SelectedUklonAddress)
                .Required(() => Resources.RequiredErrorMessage);
        }

        public override void OnClose(CancelEventArgs e)
        {
            base.OnClose(e);

            cancellationTokenSource.Cancel();
        }

        protected override async Task HandleLoadedAsync()
        {
            UklonOrderParameter parameter = (UklonOrderParameter)Parameter;

            OrderId = parameter.OrderId;

            OrderDto order = await WebClient.ExecuteApiRequestAsync(new QueryOrder(parameter.OrderId));

            _order = order;

            DocumentId = order.PackageTtn;

            if (!string.IsNullOrWhiteSpace(DocumentId))
            {
                UklonDocumentDto uklonDocumentDto = await WebClient.ExecuteApiRequestAsync(new QueryUklonDocument(DocumentId));

                MapDto(uklonDocumentDto);

                Title = "Доставка Uklon";
            }
            else
            {
                Title = "Создание доставки Uklon";
            }

            Address = order.Address;

            UklonDocumentStates = Dictionaries.GetItems<UklonDocumentState>().ToReadOnlyObservableCollection();

            // do not await
            RefreshAsync(cancellationTokenSource.Token);

            await base.HandleLoadedAsync();
        }

        protected override Task HandleOkAsync()
        {
            CloseOk();

            return Task.CompletedTask;
        }

        private async Task SearchUklonDriverAsync()
        {
            CreateUklonOrderDto createUklonOrderDto = new CreateUklonOrderDto()
            {
                OrderId = _order.Id,
                UklonAddressId = ReceiverUklonAddressId,
                UklonAddressName = ReceiverUklonAddressName
            };

            Result<UklonDocumentDto> result = await _errorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new CreateUklonOrder(createUklonOrderDto)),
                "создании доставки Uklon",
                "Доставка создана",
                this,
                true);

            if (result?.IsSuccess == true)
            {
                DocumentId = result.Data.Id;

                MapDto(result.Data);
            }
        }

        private async Task SearchUklonAddressAsync()
        {
            if (CanCreateUklonOrder)
            {
                Result<UklonAddressesDto> result = await _errorHandler.HandleErrorsAsync(
                    _ => WebClient.ExecuteApiRequestAsync(new SearchUklonAddresses(new SearchUklonAddresses.SearchUklonAddressesDto()
                    {
                        Address = Address,
                        CityId = _order.CityId!.Value
                    })),
                    "запросе адресов Uklon",
                    null,
                    this,
                    true);

                if (result?.IsSuccess != true)
                {
                    return;
                }

                UklonAddresses = result.Data.Addresses
                            .Select((x, i) => new ComboBoxItem(i, x.Name, true, x.Id))
                            .ToObservableCollection();
            }
        }

        private async Task CancelUklonOrderAsync()
        {
            Result<object> result = await _errorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new CancelUklonOrder(DocumentId, new CancelUklonOrderDto()
                {
                   OrderId = _order.Id
                })),
                "отмене заказа Uklon",
                "Заказ в Uklon отменен",
                this,
                true);

            if (result?.IsSuccess != true)
            {
                return;
            }

            CloseOk();
        }

        private async Task RefreshAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);

                if (!string.IsNullOrWhiteSpace(DocumentId))
                {
                    UklonDocumentDto uklonDocumentDto = await WebClient.ExecuteApiRequestAsync(new QueryUklonDocument(DocumentId));

                    MapDto(uklonDocumentDto);
                }
            }
        }

        private void MapDto(UklonDocumentDto uklonDocumentDto)
        {
            UklonDocumentStateId = uklonDocumentDto.StateId;
            DriverName = uklonDocumentDto.Driver?.Name;
            DriverHasProblemsWithHearing = uklonDocumentDto.Driver?.DisabilityType is UklonDriverDisabilityType.HardHearing or UklonDriverDisabilityType.Deaf;
            DriverCompletedOrders = uklonDocumentDto.Driver?.CompletedOrders is null or 0 ? null : uklonDocumentDto.Driver.CompletedOrders;
            DriverPhone = uklonDocumentDto.Driver?.Phone;
            CarModel = uklonDocumentDto.Car?.Model;
            CarBrand = uklonDocumentDto.Car?.Brand;
            CarColor = uklonDocumentDto.Car?.Color;
            CarLicencePlate = uklonDocumentDto.Car?.LicensePlate;

            if (!string.IsNullOrWhiteSpace(DocumentId))
            {
                UklonAddresses = new[] { new ComboBoxItem(0, uklonDocumentDto.ReceiverAddress?.Name, true, uklonDocumentDto.ReceiverAddress?.Id) }.ToObservableCollection();
                SelectedUklonAddress = UklonAddresses.FirstOrDefault();
            }
        }

        private bool CanCancelUklonOrder()
        {
            return UklonDocumentStateId is not null
                   && UklonDocumentStateId != UklonDocumentState.Completed.Id
                   && UklonDocumentStateId != UklonDocumentState.Running.Id
                   && UklonDocumentStateId != UklonDocumentState.Returning.Id
                   && UklonDocumentStateId != UklonDocumentState.Canceled.Id;
        }

        private void OpenOrder()
        {
            _messenger.Send(new OrderEditViewMessage(OrderId));
        }

        #region IDisposable Support
        private bool disposedValue; // To detect redundant calls

        public void Dispose()
        {
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    cancellationTokenSource.Dispose();
                }

                disposedValue = true;
            }
        }

        #endregion
    }
}