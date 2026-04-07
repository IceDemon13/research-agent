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
using Telemart.Client.Data.Requests.Features.Novaposhta.Actions;
using Telemart.Client.Data.Requests.Features.Products;
using Telemart.Client.Data.Requests.Features.TradeIn;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Mediator.Requests;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects.TradeIn;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Store;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.TradeIn
{
    public sealed class TradeInCreateTtnViewModel : TelemartDialogViewModelBase
    {
        private TradeInDto tradeIn;

        private IErrorHandler ErrorHandler { get; }

        private IMediator Mediator { get; }

        public TradeInCreateTtnViewModel(
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

        #region INPC

        public ReadOnlyObservableCollection<TradeInCreateTrackNumberSourceViewItem> Sources
        {
            get { return GetProperty(() => Sources); }
            private set { SetProperty(() => Sources, value); }
        }

        public TradeInCreateTrackNumberSourceViewItem CurrentSource
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

        public IAsyncCommand CreateTrackNumberCommand { get; set; }

        public static void BuildMetadata(MetadataBuilder<TradeInCreateTtnViewModel> builder)
        {
            builder.Property(x => x.CurrentSource)
                .Required(() => Resources.RequiredErrorMessage);

            builder.Property(x => x.TrackNumber)
                .MatchesInstanceRule(
                    (x, y) => string.IsNullOrWhiteSpace(y.CarryType.TtnRegex) || (x != null && Regex.IsMatch(x, y.CarryType.TtnRegex)),
                    () => "Введите корректно номер ТТН");
        }

        protected override async Task HandleLoadedAsync()
        {
            CarryType = Dictionaries.GetItemById<CarryType>(CarryType.NpWarehouseId);

            Title = "Введите ТТН";

            var tradeInId = (int)Parameter;

            tradeIn = await WebClient.ExecuteApiRequestAsync(new QueryTradeIn(tradeInId));

            List<TradeInCreateTrackNumberSourceDto> sources = await WebClient.ExecuteApiRequestAsync(new QueryTradeInTrackNumberSources(tradeInId));

            Sources = sources.Select(x => new TradeInCreateTrackNumberSourceViewItem
            {
                Source = x.Source,
                Recipient = x.Recipient,
                Phone = x.Phone,
                CarryId = x.CarryId,
                DeliveryData = x.DeliveryData,
            })
            .ToReadOnlyObservableCollection();
        }

        protected override Task HandleOkAsync()
        {
            IsOk = true;
            Close();

            return Task.CompletedTask;
        }

        private async Task CreateTrackNumberAsync()
        {
            PackageProperties packageProperties = await GetPackagePropertiesAsync();

            if (packageProperties == null)
            {
                return;
            }

            string packageTtn = string.Empty;

            CreateTradeInTrackNumber gatewayRequestResult = new CreateTradeInTrackNumber(
                tradeIn.Id,
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

            Result<TradeInCreateTrackNumberResultDto> result = await ErrorHandler.HandleErrorsAsync(
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
                Logger.LogError(ex, "Failed to print ttn for service invoice");
                MessageFacadeService.ShowNotificationError("Ошибка при печати ТТН. Попробуйте снова");
            }
        }

        private async Task<PackageProperties> GetPackagePropertiesAsync()
        {
            PackageProperties packageProperties = null;

            if (!(tradeIn.CarryOutId is CarryType.PickupId or CarryType.UklonId))
            {
                var carry = Dictionaries.GetItemById<CarryType>(tradeIn.CarryOutId.Value);

                PackageMaxDimensionsParameter maxDimensionsOrder = null;
                List<PackagePropertiesProductParameter> productParameter = new List<PackagePropertiesProductParameter>(1);

                decimal insurance = tradeIn.RealBuyoutAmount ?? 0;
                decimal weight = 0;

                if (tradeIn.ProductId.HasValue)
                {
                    var product = (await WebClient.ExecuteApiRequestAsync(new QueryProductsSimple([tradeIn.ProductId.Value]))).FirstOrDefault();

                    if (product != null)
                    {
                        int width = (int)Math.Ceiling((decimal)(product.Width ?? 0) / 10);

                        int heigth = (int)Math.Ceiling((decimal)(product.Height ?? 0) / 10);

                        int depth = (int)Math.Ceiling((decimal)(product.Depth ?? 0) / 10);

                        weight = (decimal)product.Weight;

                        maxDimensionsOrder = new PackageMaxDimensionsParameter(heigth, width, depth);

                        productParameter.Add(new PackagePropertiesProductParameter(heigth, width, depth));
                    }
                }
                else
                {
                    productParameter.Add(new PackagePropertiesProductParameter(0, 0, 0));
                }

                PackagePropertiesViewModel dialogViewModel = SizeableDialogDocumentManagerService.ShowView<PackagePropertiesViewModel>(
                    new PackagePropertiesParameter(
                        1,
                        insurance,
                        tradeIn.CarryOutId.Value,
                        weight,
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