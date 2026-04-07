using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using MediatR;
using Microsoft.Extensions.Logging;
using Telemart.Client.Business.Delivery.Data;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.City;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.Requests.Features.Novaposhta.Actions;
using Telemart.Client.Data.Requests.Features.Products;
using Telemart.Client.Data.Requests.Features.ServiceCenter;
using Telemart.Client.Data.Requests.Features.ServiceInvoice;
using Telemart.Client.Data.Requests.Features.ServiceInvoice.Actions;
using Telemart.Client.Data.Requests.Features.ServiceRepair;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Mediator.Requests;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.City;
using Telemart.Client.TransferObjects.Novaposhta;
using Telemart.Client.TransferObjects.Warehouse;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Service.ServiceRepairs;
using Telemart.Client.ViewModels.Store;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Service.ServiceInvoices
{
    public sealed class SendServiceInvoiceViewModel : TelemartDialogViewModelBase
    {
        private int serviceInvoiceId;
        private ServiceInvoiceDto serviceInvoice;
        private List<(ProductSimpleDto Product, int Quantity)> serviceInvoiceProducts;

        public SendServiceInvoiceViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMessenger messenger,
            IErrorHandler errorHandler,
            IMediator mediator)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
            ErrorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
            Mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            CreateTrackNumberCommand = new AsyncCommand(CreateTrackNumberAsync);
        }

        public SendServiceInvoiceViewModel()
        {
        }

        #region Commands

        public IAsyncCommand CreateTrackNumberCommand { get; }

        #endregion

        #region INPC

        public int? SelectedEmployeeCarrierId
        {
            get { return GetProperty(() => SelectedEmployeeCarrierId); }
            set { SetProperty(() => SelectedEmployeeCarrierId, value); }
        }

        public string TrackNumber
        {
            get { return GetProperty(() => TrackNumber); }
            set { SetProperty(() => TrackNumber, value); }
        }

        public List<EmployeeDto> Employees
        {
            get { return GetProperty(() => Employees); }
            set { SetProperty(() => Employees, value); }
        }

        public bool NeedEmployeeCarrier
        {
            get { return GetProperty(() => NeedEmployeeCarrier); }
            set { SetProperty(() => NeedEmployeeCarrier, value, () => { RaisePropertyChanged(nameof(SelectedEmployeeCarrierId)); }); }
        }

        public ServiceCenterDto ServiceCenter
        {
            get { return GetProperty(() => ServiceCenter); }
            private set { SetProperty(() => ServiceCenter, value); }
        }

        public bool? SendToLegal
        {
            get { return GetProperty(() => SendToLegal); }
            set { SetProperty(() => SendToLegal, value); }
        }

        #endregion

        private IMessenger Messenger { get; }

        private IErrorHandler ErrorHandler { get; }

        private IMediator Mediator { get; }

        public static void BuildMetadata(MetadataBuilder<SendServiceInvoiceViewModel> builder)
        {
            builder.Property(x => x.SelectedEmployeeCarrierId)
                .MatchesInstanceRule((x, y) => !y.NeedEmployeeCarrier || x.HasValue, () => Resources.RequiredErrorMessage);
        }

        protected override async Task HandleLoadedAsync()
        {
            SendServiceInvoiceParameter p = (SendServiceInvoiceParameter)Parameter;

            serviceInvoiceId = p.ServiceInvoiceId;

            var filteringItem = new RepairFilteringItem
            {
                Invoices = serviceInvoiceId.ToString()
            };

            serviceInvoice = await WebClient.ExecuteApiRequestAsync(new QueryServiceInvoice(serviceInvoiceId));
            List<ServiceRepairDto> serviceRepairs = await WebClient.ExecuteApiRequestAsync(new QueryServiceRepairs(filteringItem)).GetPagedResultDataAsync();

            var products = await WebClient.ExecuteApiRequestAsync(new QueryProductsSimple(serviceRepairs.Select(x => x.ProductId).ToArray()));

            ServiceCenter = await WebClient.ExecuteApiRequestAsync(new QueryServiceCenter(serviceInvoice.ServiceCenterId));

            if (!string.IsNullOrWhiteSpace(ServiceCenter.Edrpou))
            {
                SendToLegal = true;
            }

            serviceInvoiceProducts = products.Select(x => (x, serviceRepairs.Count(y => y.ProductId == x.Id))).ToList();

            List<EmployeeDto> employees = await WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true).GetPagedResultDataAsync();
            List<WarehouseDto> warehouses = await WebClient.ExecuteApiRequestAsync(new QueryWarehouses(), true).GetPagedResultDataAsync();
            List<CityDto> cities = await WebClient.ExecuteApiRequestAsync(new QueryCities(), true).GetPagedResultDataAsync();

            WarehouseDto invoiceWarehouse = warehouses.First(x => x.Id == p.WarehouseId);
            CityDto invoiceWarehouseCity = cities.First(x => x.Id == invoiceWarehouse.CityId);

            Employees = employees.Where(x => x.Active && x.CityId == invoiceWarehouseCity.Id).OrderBy(x => x.Name).ToList();
            SelectedEmployeeCarrierId = p.EmployeeCarrierId;
            TrackNumber = p.Ttn;
            NeedEmployeeCarrier = p.NeedEmployeeCarrier;

            Title = $"Серв. накладная №{serviceInvoiceId}";
        }

        protected override async Task HandleOkAsync()
        {
            if (IDataErrorInfoHelper.HasErrors(this))
            {
                return;
            }

            try
            {
                SendServiceInvoiceDto sendServiceInvoiceDto = NeedEmployeeCarrier ?
                    new SendServiceInvoiceDto { EmployeeCarrierId = SelectedEmployeeCarrierId.Value, Ttn = TrackNumber } :
                    new SendServiceInvoiceDto();

                ServiceInvoiceDto serviceInvoiceDto = await WebClient.ExecuteApiRequestAsync(new SendServiceInvoice(serviceInvoiceId, sendServiceInvoiceDto));

                MessageFacadeService.ShowNotificationInfo("Серв. накладная успешно отправлена");
                Messenger.Send(new ServiceInvoiceMessage(serviceInvoiceDto, MessageType.Changed));

                IsOk = true;
                Close();
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при отправке серв. накладной");
                ShowValidationResultView("Ошибки при отправке серв. накладной", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to send service invoice");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to send service invoice. ServiceInvoiceId: {ServiceInvoiceId}", serviceInvoiceId);
                MessageFacadeService.ShowNotificationError("Ошибка при отправке серв. накладной");
            }
        }

        private async Task CreateTrackNumberAsync()
        {
            PackageProperties packageProperties = GetPackageProperties();

            if (packageProperties == null)
            {
                return;
            }

            string packageTtn = string.Empty;

            var createTtnRequest = new CreateNpTtnByServiceInvoice(
                serviceInvoiceId,
                packageProperties.Places.Count,
                (double)packageProperties.TotalWeight,
                packageProperties.TotalInsurance,
                packageProperties.AddToApplication,
                packageProperties.Places.First().Width,
                packageProperties.Places.First().Length,
                packageProperties.Places.First().Height,
                SendToLegal);

            Result<NpDocumentDto> result = await ErrorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(createTtnRequest),
                "создании ТТН",
                "ТТН создана",
                this,
                true);

            if (result?.Data is null)
            {
                return;
            }

            try
            {
                await Mediator.Send(new PrintTrackNumberRequest(result.Data.Id, result.Data.Link, true));
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to print ttn for service invoice");
                MessageFacadeService.ShowNotificationError("Ошибка при печати ТТН. Попробуйте снова");
            }

            TrackNumber = result.Data.Id;
        }

        private PackageProperties GetPackageProperties()
        {
            PackageProperties packageProperties = null;

            if (!(serviceInvoice.CarryId is CarryType.PickupId or CarryType.UklonId))
            {
                var carry = Dictionaries.GetItemById<CarryType>(serviceInvoice.CarryId);

                PackageMaxDimensionsParameter maxDimensionsOrder = null;

                decimal insurance = serviceInvoiceProducts.Sum(x => (x.Product.Prices?.Select(y => y.Price).Max() ?? 0) * x.Quantity) * carry.InsurancePercent / 100;

                if (serviceInvoiceProducts.Count > 0)
                {
                    int width = serviceInvoiceProducts.Select(x => (int)Math.Ceiling((decimal)(x.Product.Width ?? 0) / 10)).Max();

                    int heigth = serviceInvoiceProducts.Select(x => (int)Math.Ceiling((decimal)(x.Product.Height ?? 0) / 10)).Max();

                    int depth = serviceInvoiceProducts.Select(x => (int)Math.Ceiling((decimal)(x.Product.Depth ?? 0) / 10)).Max();

                    maxDimensionsOrder = new PackageMaxDimensionsParameter(heigth, width, depth);
                }

                PackagePropertiesViewModel dialogViewModel = SizeableDialogDocumentManagerService.ShowView<PackagePropertiesViewModel>(
                    new PackagePropertiesParameter(
                        1,
                        insurance,
                        serviceInvoice.CarryId,
                        (decimal)serviceInvoiceProducts.Sum(x => x.Product.Weight),
                        maxDimensionsParameter: maxDimensionsOrder,
                        products: serviceInvoiceProducts.Select(x => new PackagePropertiesProductParameter(x.Product.Height, x.Product.Width, x.Product.Depth)).ToArray()),
                    this);

                if (dialogViewModel.IsOk)
                {
                    packageProperties = new PackageProperties(dialogViewModel.PackagePlaceItems.Select(x => new PackagePlaceProperties(x.Weight, x.Insurance, x.Length, x.Height, x.Width)), true)
                    {
                        AddToApplication = !dialogViewModel.NotAddToNpApplication
                    };
                }
            }
            else
            {
                packageProperties = new PackageProperties(new[] { new PackagePlaceProperties(1, 0) }, true);
            }

            return packageProperties;
        }
    }
}