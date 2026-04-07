using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using MediatR;
using Microsoft.Extensions.Logging;
using Telemart.Client.Business.Delivery.Data;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.Order;
using Telemart.Client.Data.Requests.Features.Products;
using Telemart.Client.Data.Requests.Features.ServiceRequest;
using Telemart.Client.Data.Requests.Features.ServiceRequest.Actions;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Mediator.Requests;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Store;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Service.ServiceRequests
{
    public sealed class CompleteServiceRequestViewModel : TelemartDialogViewModelBase
    {
        private ServiceRequestDto serviceRequest;

        private IErrorHandler ErrorHandler { get; }

        private IMediator Mediator { get; }

        public CompleteServiceRequestViewModel(
            IWebClient webClient,
            IErrorHandler errorHandler,
            IMediator mediator,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
            ErrorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
            Mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));

            CreateTrackNumberCommand = new AsyncCommand(CreateTrackNumberAsync, () => CurrentSource != null);
        }

        public CompleteServiceRequestViewModel()
        {
        }

        #region Commands

        public IAsyncCommand CreateTrackNumberCommand { get; }

        #endregion

        #region INPC

        public ReadOnlyObservableCollection<CreateServiceRequestTrackNumberSourceViewItem> Sources
        {
            get { return GetProperty(() => Sources); }
            private set { SetProperty(() => Sources, value); }
        }

        public CreateServiceRequestTrackNumberSourceViewItem CurrentSource
        {
            get { return GetProperty(() => CurrentSource); }
            set { SetProperty(() => CurrentSource, value); }
        }

        public string TrackNumber
        {
            get { return GetProperty(() => TrackNumber); }
            set { SetProperty(() => TrackNumber, value); }
        }

        public CarryType CarryType
        {
            get { return GetProperty(() => CarryType); }
            private set { SetProperty(() => CarryType, value, () => { RaisePropertyChanged(nameof(TrackNumber)); }); }
        }

        #endregion

        public static void BuildMetadata(MetadataBuilder<CompleteServiceRequestViewModel> builder)
        {
            builder.Property(x => x.CurrentSource)
                .Required(() => Resources.RequiredErrorMessage);

            builder.Property(x => x.TrackNumber)
                .MatchesInstanceRule(
                    (x, y) => string.IsNullOrWhiteSpace(y.CarryType?.TtnRegex) || (x != null && Regex.IsMatch(x, y.CarryType?.TtnRegex)),
                    () => "Введите корректно номер ТТН");
        }

        protected override async Task HandleLoadedAsync()
        {
            var serviceRequestId = (int)Parameter;

            serviceRequest = await WebClient.ExecuteApiRequestAsync(new QueryServiceRequest(serviceRequestId));

            CarryType = Dictionaries.GetItemById<CarryType>(CarryType.NpWarehouseId);

            Title = "Введите ТТН";

            List<CreateServiceRequestTrackNumberSourceDto> sources = await WebClient.ExecuteApiRequestAsync(new QueryServiceRequestTrackNumberSources(serviceRequestId));

            Sources = sources
                .Where(x => x.CarryId.HasValue)
                .Select(x => new CreateServiceRequestTrackNumberSourceViewItem
                {
                    Source = x.Source,
                    Recipient = x.Recipient,
                    Phone = x.Phone,
                    CarryId = x.CarryId ?? 0,
                    DeliveryData = x.DeliveryData
                })
                .ToReadOnlyObservableCollection();
        }

        protected override async Task HandleOkAsync()
        {
            if (IDataErrorInfoHelper.HasErrors(this))
            {
                return;
            }

            try
            {
                Result<ServiceRequestDto> result = await WebClient.ExecuteApiRequestAsync(new CompleteServiceRequest(serviceRequest.Id, TrackNumber));

                if (result.Warnings.Any())
                {
                    MessageFacadeService.ShowNotificationInfo($"Заявка №{serviceRequest.Id} завершена c предупреждениями");
                    ShowValidationResultView("Предупрежедения", result.Warnings.Select(x => new ValidationResultItem(x, false)).ToList());
                }
                else
                {
                    MessageFacadeService.ShowNotificationInfo($"Заявка №{serviceRequest.Id} успешно завершена");
                }

                IsOk = true;
                Close();
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при завершении заявки");
                ShowValidationResultView("Ошибки при завершении заявки", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to complete service request");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при завершении заявки");
                Logger.LogError(exception, "Failed to complete service request");
            }
        }

        private async Task CreateTrackNumberAsync()
        {
            PackageProperties packageProperties = await GetPackagePropertiesAsync();

            if (packageProperties == null)
            {
                return;
            }

            string packageTtn = string.Empty;

            CreateServiceRequestTrackNumber gatewayRequestResult = new CreateServiceRequestTrackNumber(
                serviceRequest.Id,
                CurrentSource.Recipient,
                CurrentSource.Phone,
                CurrentSource.CarryId,
                CurrentSource.DeliveryData,
                packageProperties.Places.Count,
                (double)packageProperties.TotalWeight,
                packageProperties.TotalInsurance,
                packageProperties.AddToApplication,
                packageProperties.Places.First().Width,
                packageProperties.Places.First().Length,
                packageProperties.Places.First().Height);

            Result<ServiceRequestCreateTrackNumberResultDto> result = await ErrorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(gatewayRequestResult),
                "создании ТТН",
                "ТТН создана",
                this,
                true);

            if (result?.Data is null)
            {
                return;
            }

            TrackNumber = result.Data.Number;

            try
            {
                await Mediator.Send(new PrintTrackNumberRequest(result.Data.Number, result.Data.Link, true));
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to print ttn for service request");
                MessageFacadeService.ShowNotificationError("Ошибка при печати ТТН. Попробуйте снова");
            }
        }

        private async Task<PackageProperties> GetPackagePropertiesAsync()
        {
            PackageProperties packageProperties = null;

            if (!(CurrentSource.CarryId is CarryType.PickupId or CarryType.UklonId))
            {
                var carry = Dictionaries.GetItemById<CarryType>(CurrentSource.CarryId);
                var order = await WebClient.ExecuteApiRequestAsync(new QueryOrder(serviceRequest.OrderId));
                var product = (await WebClient.ExecuteApiRequestAsync(new QueryProductsSimple([serviceRequest.ProductId]))).FirstOrDefault();

                decimal insurance = order.Products.Select(x => x.PriceOut).Max() * carry.InsurancePercent / 100;

                PackageMaxDimensionsParameter maxDimensionsOrder = null;
                List<PackagePropertiesProductParameter> productParameter = new List<PackagePropertiesProductParameter>(1);

                if (product != null)
                {
                    int width = (int)Math.Ceiling((decimal)(product.Width ?? 0) / 10);

                    int heigth = (int)Math.Ceiling((decimal)(product.Height ?? 0) / 10);

                    int depth = (int)Math.Ceiling((decimal)(product.Depth ?? 0) / 10);

                    maxDimensionsOrder = new PackageMaxDimensionsParameter(heigth, width, depth);

                    productParameter.Add(new PackagePropertiesProductParameter(heigth, width, depth));
                }

                PackagePropertiesViewModel dialogViewModel = SizeableDialogDocumentManagerService.ShowView<PackagePropertiesViewModel>(
                    new PackagePropertiesParameter(
                        1,
                        insurance,
                        CurrentSource.CarryId,
                        (decimal)product.Weight,
                        maxDimensionsParameter: maxDimensionsOrder,
                        products: productParameter,
                        allowEditInsurance: true),
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