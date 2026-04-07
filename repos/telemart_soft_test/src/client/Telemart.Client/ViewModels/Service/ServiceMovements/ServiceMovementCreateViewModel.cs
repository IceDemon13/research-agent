using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using MediatR;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.Calculators;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Core.Extensions;
using Telemart.Client.Data.Requests.Features.Carry;
using Telemart.Client.Data.Requests.Features.Novaposhta.Actions;
using Telemart.Client.Data.Requests.Features.ServiceMovement;
using Telemart.Client.Data.Requests.Features.ServiceRequest;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Mediator.Requests;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.TransferObjects.ServiceMovement;
using Telemart.Client.TransferObjects.Warehouse;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Common.RecognizeServiceBarcode;
using Telemart.Client.ViewModels.Service.ServiceRequests;
using Telemart.Client.ViewModels.Store;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Service.ServiceMovements
{
    public sealed class ServiceMovementCreateViewModel : TelemartDialogViewModelBase
    {
        private List<DeliveryTypeDto> deliveryTypes;

        public ServiceMovementCreateViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            IMessenger messenger,
            IErrorHandler errorHandler,
            IMediator mediator,
            IInsuranceCalculator calculatorInsurance,
            RecognizeServiceBarcodeViewModel recognizeServiceBarcodeViewModel)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
            Mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

            DeleteCommand = new DelegateCommand(Delete, () => SelectedProduct != null);
            ProcessAllCommand = new DelegateCommand(ProcessAll);
            FillCommand = new AsyncCommand(FillAsync, () => WarehouseFromId.HasValue);

            RecognizeServiceBarcodeViewModel = recognizeServiceBarcodeViewModel;

            RecognizeServiceBarcodeViewModel.OnFinished += RecognizeServiceBarcodeViewModelOnFinished;

            ErrorHandler = errorHandler;
            Mediator = mediator;
            CalculatorInsurance = calculatorInsurance;
        }

        public ServiceMovementCreateViewModel()
        {
        }

        public static DateTime MinDate => DateTime.Now.Date;

        public static DateTime MaxDate => DateTime.Now.AddDays(3).Date;

        public RecognizeServiceBarcodeViewModel RecognizeServiceBarcodeViewModel { get; }

        #region Commands

        public IDelegateCommand DeleteCommand
        {
            get { return GetProperty(() => DeleteCommand); }
            private set { SetProperty(() => DeleteCommand, value); }
        }

        public IDelegateCommand ProcessAllCommand
        {
            get { return GetProperty(() => ProcessAllCommand); }
            private set { SetProperty(() => ProcessAllCommand, value); }
        }

        public IAsyncCommand FillCommand
        {
            get { return GetProperty(() => FillCommand); }
            private set { SetProperty(() => FillCommand, value); }
        }

        #endregion

        #region INPC

        public ReadOnlyObservableCollection<ComboBoxItem> FromWarehouses
        {
            get { return GetProperty(() => FromWarehouses); }
            private set { SetProperty(() => FromWarehouses, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> ToWarehouses
        {
            get { return GetProperty(() => ToWarehouses); }
            private set { SetProperty(() => ToWarehouses, value); }
        }

        public ObservableCollection<ServiceMovementProductViewItem> Products
        {
            get { return GetProperty(() => Products); }
            set { SetProperty(() => Products, value); }
        }

        public ServiceMovementProductViewItem SelectedProduct
        {
            get { return GetProperty(() => SelectedProduct); }
            set { SetProperty(() => SelectedProduct, value); }
        }

        public int? WarehouseFromId
        {
            get { return GetProperty(() => WarehouseFromId); }
            set { SetProperty(() => WarehouseFromId, value); }
        }

        public int? WarehouseToId
        {
            get { return GetProperty(() => WarehouseToId); }
            set { SetProperty(() => WarehouseToId, value); }
        }

        public int? CarryId
        {
            get { return GetProperty(() => CarryId); }
            set { SetProperty(() => CarryId, value, ChangeCarryType); }
        }

        public int? DeliveryTypeId
        {
            get { return GetProperty(() => DeliveryTypeId); }
            set { SetProperty(() => DeliveryTypeId, value); }
        }

        public DateTime? DateReceive
        {
            get { return GetProperty(() => DateReceive); }
            set { SetProperty(() => DateReceive, value); }
        }

        public bool ReadonlyDeliveryType
        {
            get { return GetProperty(() => ReadonlyDeliveryType); }
            set { SetProperty(() => ReadonlyDeliveryType, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> CarryTypes
        {
            get { return GetProperty(() => CarryTypes); }
            private set { SetProperty(() => CarryTypes, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> DeliveryTypes
        {
            get { return GetProperty(() => DeliveryTypes); }
            private set { SetProperty(() => DeliveryTypes, value); }
        }

        #endregion

        public override int Width => 600;

        public override int Height => 400;

        private IMapper Mapper { get; }

        private IMessenger Messenger { get; }

        private IErrorHandler ErrorHandler { get; }

        private IMediator Mediator { get; }

        private IInsuranceCalculator CalculatorInsurance { get; }

        public static void BuildMetadata(MetadataBuilder<ServiceMovementCreateViewModel> builder)
        {
            builder.Property(x => x.WarehouseFromId).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.WarehouseToId).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.DateReceive).Required(() => Resources.RequiredErrorMessage);
        }

        protected override async Task HandleLoadedAsync()
        {
            Products = new ObservableCollection<ServiceMovementProductViewItem>();

            List<WarehouseDto> warehouses = await WebClient.ExecuteApiRequestAsync(new QueryWarehouses(), true).GetPagedResultDataAsync();

            FromWarehouses = warehouses
                .Where(x => x.Active > 0 && WebClient.AuthenticatedEmployee.AllowWarehouses.Contains(x.Id))
                .OrderByDescending(x => x.Position)
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToReadOnlyObservableCollection();

            ToWarehouses = warehouses
                .Where(x => x.Active > 0 && (x.TypeId == WarehouseKind.Pickup.Id || x.TypeId == WarehouseKind.Service.Id))
                .OrderByDescending(x => x.Position)
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToReadOnlyObservableCollection();

            CarryTypes = Dictionaries.GetItems<CarryType>().Where(x => x.UseInServiceMovement)
                .Select(x => new ComboBoxItem(x.Id, x.Name)).ToReadOnlyObservableCollection();

            deliveryTypes = await WebClient.ExecuteApiRequestAsync(new QueryDeliveryTypes());

            ChangeCarryType();

            await base.HandleLoadedAsync();

            Title = "Создание сервисного перемещения";
        }

        protected override async Task HandleOkAsync()
        {
            if (!Products.Any())
            {
                MessageFacadeService.ShowNotificationWarning("Нечего сохранять");
                return;
            }

            if (Products.Any(x => x.IsError || !x.IsProcessed))
            {
                MessageFacadeService.ShowNotificationWarning("Все заявки должны быть распознаны");
                return;
            }

            if (Products.Any(x => !x.Scanned))
            {
                MessageFacadeService.ShowNotificationError("Все заявки должны быть просканированы");
                return;
            }

            PackagePropertiesViewModel packagePropertiesViewModel = null;
            int places = 0;

            if (CarryId == CarryType.NpWarehouseId || CarryId == CarryType.NpDeliveryId)
            {
                decimal insurance = await CalculatorInsurance.CalculateByServiceMovementAsync(Products.ToArray());

                packagePropertiesViewModel = GetPackageProperties(insurance);

                if (!packagePropertiesViewModel.IsOk)
                {
                    return;
                }

                places = (int)packagePropertiesViewModel.PackagePlaces;
            }

            Result<ServiceMovementDto> result = await ErrorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new CreateServiceMovement(
                    Mapper.Map<ServiceMovementCreateDto>(this))),
                "создании сервисного перемещения",
                "Сервисное перемещение создано",
                this,
                true);

            if (result?.Data is null)
            {
                return;
            }

            ServiceMovementDto resultServiceMovemen = result.Data;

            if (CarryId == CarryType.NpWarehouseId || CarryId == CarryType.NpDeliveryId)
            {
                ServiceMovementNpDocumentDto serviceMovementNpDocumentDto = await CreateTtnAsync(resultServiceMovemen.Id, places, (double)packagePropertiesViewModel.TotalWeight, packagePropertiesViewModel.Insurance, !packagePropertiesViewModel.NotAddToNpApplication);

                if (serviceMovementNpDocumentDto is not null)
                {
                    resultServiceMovemen.TrackNumber = serviceMovementNpDocumentDto.TrackNumber;
                }
            }

            Messenger.Send(new ServiceMovementMessage(result.Data, MessageType.Added));

            IsOk = true;
            Close();
        }

        private async Task FillAsync()
        {
            if (!MessageFacadeService.Confirm("Вы точно хотите заполнить?"))
            {
                return;
            }

            ServiceRequestFilteringItem filter = new ServiceRequestFilteringItem()
            {
                States = new[] { ServiceRequestState.Accepted.Id },
                WarehouseLocationId = WarehouseFromId,
                Location = ServiceRequestLocation.WarehouseId
            };

            PagedResult<ServiceRequestDto> serviceRequests = await WebClient.ExecuteApiRequestAsync(new QueryServiceRequests(filter));

            if (!serviceRequests.Data.Any())
            {
                MessageFacadeService.ShowNotificationInfo("Нечего заполнять");
                return;
            }

            foreach (ServiceRequestDto serviceRequest in serviceRequests.Data)
            {
                Products.Add(new ServiceMovementProductViewItem(
                    serviceRequest.Id,
                    serviceRequest.ProductName.GetStringWithPrefix(serviceRequest.ProductPrefixRus),
                    serviceRequest.ProductFullNameUkr,
                    serviceRequest.ProductNameEn.GetStringWithPrefix(serviceRequest.ProductPrefixEn),
                    serviceRequest.SerialNumber));
            }

            Products = Products.GroupBy(x => x.ServiceRequestId).Select(x => x.First()).ToObservableCollection();
        }

        private void Delete()
        {
            if (MessageFacadeService.Confirm("Вы уверены, что хотите удалить сервисную заявку из перемещения?"))
            {
                Products.Remove(SelectedProduct);
            }
        }

        private void ProcessAll()
        {
            Products.Where(x => !x.IsProcessed || x.IsError).ForEach(ProcessItem);
        }

        private void ProcessItem(ServiceMovementProductViewItem item)
        {
            Task.Factory.StartNew(
                async () =>
                {
                    item.IsProcessing = true;
                    try
                    {
                        ServiceRequestDto transferObject = await WebClient.ExecuteApiRequestAsync(new QueryServiceRequest(item.ServiceRequestId));

                        item.ProductFullName = transferObject.ProductName.GetStringWithPrefix(transferObject.ProductPrefixRus);

                        item.ProductSn = transferObject.SerialNumber;

                        item.Price = transferObject.PurchasedPrice;

                        item.CurrencyId = transferObject.PurchasedCurrencyId;
                    }
                    catch (UnexpectedSatusException)
                    {
                        MessageFacadeService.ShowNotificationError("Ошибка при обработке сервисной заявки");
                        item.ErrorMessage = "Ошибка при обработке сервисной заявки";
                    }
                    catch (UnexpectedErrorException exception)
                    {
                        Logger.LogError(exception, "Failed to process service request");
                        item.ErrorMessage = Resources.ServerConnectError;
                    }
                    catch (Exception exception)
                    {
                        MessageFacadeService.ShowNotificationError("Ошибка при обработке сервисной заявки");
                        Logger.LogError(exception, "Error while processing service request");
                        item.ErrorMessage = "Ошибка при обработке сервисной заявки";
                    }

                    item.IsProcessing = false;
                    item.IsProcessed = true;
                    item.Scanned = true;

                    MessageFacadeService.ShowNotificationInfo("Товар успешно добавлен");
                },
                CancellationToken.None,
                TaskCreationOptions.None,
                TaskScheduler.FromCurrentSynchronizationContext());
        }

        private void RecognizeServiceBarcodeViewModelOnFinished(object sender, RecognizeServiceBarcodeResultEventArgs e)
        {
            if (e.IsValid && e.ServiceRequestId.HasValue)
            {
                ServiceMovementProductViewItem item = Products.FirstOrDefault(x => x.ServiceRequestId == e.ServiceRequestId);

                if (item is null)
                {
                    item = new ServiceMovementProductViewItem(e.ServiceRequestId.Value);

                    Products.Add(item);
                    ProcessItem(item);

                    return;
                }

                if (item.Scanned)
                {
                    MessageFacadeService.ShowNotificationWarning($"Сервисная заявка {e.ServiceRequestId} уже просканирована");
                }
                else
                {
                    item.Scanned = true;
                }
            }
            else
            {
                MessageFacadeService.ShowNotificationWarning(e.ErrorText, true);
            }
        }

        private void ChangeCarryType()
        {
            DeliveryTypes = deliveryTypes.Where(x => CarryId == x.CarryId).Select(x => new ComboBoxItem(x.Id, x.Name)).ToReadOnlyObservableCollection();

            ReadonlyDeliveryType = DeliveryTypes.Count == 0;

            if (DeliveryTypes.Count > 0)
            {
                DeliveryTypeId = deliveryTypes.FirstOrDefault(x => CarryId == x.CarryId)?.Id;
            }
        }

        private PackagePropertiesViewModel GetPackageProperties(decimal insurance)
        {
            return SizeableDialogDocumentManagerService.ShowView<PackagePropertiesViewModel>(new PackagePropertiesParameter(0, insurance, CarryId.Value), this);
        }

        private async Task<ServiceMovementNpDocumentDto> CreateTtnAsync(int idServiceMovement, int places, double weight, decimal insurance, bool addToNpApllication)
        {
            Result<ServiceMovementNpDocumentDto> result = await ErrorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new CreateNpTtnByServiceMovement(idServiceMovement, places, weight, insurance, addToNpApllication)),
                "создании ТТН",
                "ТТН создана",
                this,
                true);

            if (result?.Data is null)
            {
                return null;
            }

            try
            {
                await Mediator.Send(new PrintTrackNumberRequest(result.Data.TrackNumber, result.Data.Link, true));
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to print ttn for service movement");
                MessageFacadeService.ShowNotificationError("Ошибка при печати ТТН. Попробуйте снова");
            }

            return result.Data;
        }
    }
}