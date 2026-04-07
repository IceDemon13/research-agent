using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using MediatR;
using Microsoft.Extensions.Logging;
using Telemart.Client.Business.Barcode;
using Telemart.Client.Business.Order;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.ReportFactory;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Core.Helpers;
using Telemart.Client.Data.Requests.Features.Products;
using Telemart.Client.Data.Requests.Features.ServiceProduct.Actions;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Mediator.Requests;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.ServiceProduct;
using Telemart.Client.TransferObjects.Warehouse;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Service.ServiceProducts
{
    public sealed class ServiceProductDiscountViewModel : TelemartDialogViewModelBase
    {
        private ServiceProductDiscountParameter parameter;

        public ServiceProductDiscountViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IOrderRules orderRules,
            IMediator mediator,
            IMessenger messenger,
            IBarcodeReportFactory barcodeReportFactory)
            : base(webClient, dictionaries, messageFacadeService)
        {
            OrderRules = orderRules;
            Mediator = mediator;
            BarcodeReportFactory = barcodeReportFactory;

            CreateDiscountProductCommand = new DelegateCommand(CreateDiscountProduct);
            NavigateToProductCommand = new DelegateCommand(() => ProcessHelper.Start(ProductDiscount.Link), () => ProductDiscount != null);
            ProductDiscountChangedCommand = new AsyncCommand(RefreshImagesAsync);

            messenger.Register<DiscountProductMessage>(this, OnDiscountProductMessage);

            Images = new ObservableCollection<ImageSource>();
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Warehouses
        {
            get { return GetProperty(() => Warehouses); }
            private set { SetProperty(() => Warehouses, value); }
        }

        public ObservableCollection<ProductCardDto> DiscountProducts
        {
            get { return GetProperty(() => DiscountProducts); }
            private set { SetProperty(() => DiscountProducts, value); }
        }

        public int WarehouseId
        {
            get { return GetProperty(() => WarehouseId); }
            private set { SetProperty(() => WarehouseId, value); }
        }

        public string ProductName
        {
            get { return GetProperty(() => ProductName); }
            private set { SetProperty(() => ProductName, value); }
        }

        public ProductCardDto ProductDiscount
        {
            get { return GetProperty(() => ProductDiscount); }
            set { SetProperty(() => ProductDiscount, value); }
        }

        public string SerialNumber
        {
            get { return GetProperty(() => SerialNumber); }
            set { SetProperty(() => SerialNumber, value); }
        }

        public string ErrorText
        {
            get { return GetProperty(() => ErrorText); }
            private set { SetProperty(() => ErrorText, value); }
        }

        public ObservableCollection<ImageSource> Images
        {
            get { return GetProperty(() => Images); }
            private set { SetProperty(() => Images, value); }
        }

        public ProductAttributesDto ProductAttributes
        {
            get { return GetProperty(() => ProductAttributes); }
            private set { SetProperty(() => ProductAttributes, value); }
        }

        public IDelegateCommand CreateDiscountProductCommand { get; }

        public IDelegateCommand NavigateToProductCommand { get; }

        public IAsyncCommand ProductDiscountChangedCommand { get; }

        private IOrderRules OrderRules { get; }

        private IMediator Mediator { get; }

        private IBarcodeReportFactory BarcodeReportFactory { get; }

        public static void BuildMetadata(MetadataBuilder<ServiceProductDiscountViewModel> builder)
        {
            builder.Property(x => x.ProductDiscount).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.SerialNumber).MatchesInstanceRule((x, y) => y.ProductAttributes?.KeepSerial != true || !string.IsNullOrWhiteSpace(x), () => Resources.RequiredErrorMessage);
        }

        protected override async Task HandleLoadedAsync()
        {
            parameter = (ServiceProductDiscountParameter)Parameter;

            await Task.WhenAll(
                RefreshWarehouses(),
                RefreshDiscountProducts(),
                RefreshProductAttributes(parameter.ProductId));

            WarehouseId = parameter.WarehouseId;

            OurServiceBarcode ourServiceBarcode = new OurServiceBarcode(parameter.SerialNumber);

            if (!string.IsNullOrWhiteSpace(parameter.SerialNumber) && (!ourServiceBarcode.IsValid || ProductAttributes?.KeepSerial == true))
            {
                SerialNumber = parameter.SerialNumber;

                RaisePropertiesChanged(nameof(SerialNumber));
            }

            Title = "Уценка товара";

            async Task RefreshWarehouses()
            {
                List<WarehouseDto> warehouses = await WebClient.ExecuteApiRequestAsync(new QueryWarehouses(), true).GetPagedResultDataAsync();

                Warehouses = warehouses
                    .Select(x => new ComboBoxItem(x.Id, x.Name))
                    .ToReadOnlyObservableCollection();
            }

            async Task RefreshDiscountProducts()
            {
                List<ProductCardDto> products = await WebClient.ExecuteApiRequestAsync(new QueryDiscountProducts(parameter.ProductId, parameter.ServiceProductTypeId));

                DiscountProducts = products
                    .OrderByDescending(x => x.ProductId)
                    .ToObservableCollection();
            }

            async Task RefreshProductAttributes(int productId)
            {
                ProductAttributes = await WebClient.ExecuteApiRequestAsync(new QueryProductAttributes(productId));

                MapProductAttributes(ProductAttributes);

                RaisePropertiesChanged(nameof(SerialNumber));
            }
        }

        protected override async Task HandleOkAsync()
        {
            if (ProductAttributes.KeepSerial && !string.IsNullOrEmpty(SerialNumber))
            {
                string errorMessage = OrderRules.ValidateSerialNumber(SerialNumber, null);

                if (!string.IsNullOrEmpty(errorMessage))
                {
                    MessageFacadeService.ShowNotificationWarning(errorMessage);
                    return;
                }
            }

            try
            {
                DiscountServiceProduct gatewayRequest = new DiscountServiceProduct(parameter.ServiceProductId, SerialNumber, WarehouseId, ProductDiscount.ProductId, parameter.DefectCreateDiscount);

                Result<ServiceProductDto> result = await WebClient.ExecuteApiRequestAsync(gatewayRequest);

                if (result.Warnings.Any())
                {
                    MessageFacadeService.ShowNotificationWarning($"Уценка №{result.Data.ProductDiscountId} создана с предупреждениями");

                    ShowValidationResultView(
                        "Предупрежедения при создании уценки",
                        result.Warnings.Select(x => new ValidationResultItem(x, false)).ToList());
                }
                else
                {
                    MessageFacadeService.ShowNotificationInfo($"Уценка №{result.Data.ProductDiscountId} успешно создана");
                }


                int printProductId;
                string printProductName;


                if (result.Data.ProductDiscountId.HasValue)
                {
                    printProductId = result.Data.ProductDiscountId.Value;
                    printProductName = result.Data.ProductDiscountNameUkr;
                }
                else
                {
                    printProductId = ProductAttributes.ProductId;
                    printProductName = ProductAttributes.NameUkr;
                }

                await PrintBarcodeAsync(printProductId, printProductName);

                IsOk = true;
                Close();
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при создании уценки");
                ShowValidationResultView("Ошибки при создании уценки", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to create discount product");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при создании уценки");
                Logger.LogError(exception, "Error while creating discount product");
            }
        }

        private async Task RefreshImagesAsync()
        {
            Images.Clear();
            int errorImageCount = 0;
            ErrorText = null;

            if (ProductDiscount?.ImageLinks?.Any() == false)
            {
                ErrorText = "Нет фото";
                Images.Add(null);
                return;
            }

            List<string> imageLinks = ProductDiscount?.ImageLinks?.ToList();

            foreach (string imageLink in imageLinks)
            {
                if (!string.IsNullOrEmpty(imageLink))
                {
                    try
                    {
                        using HttpClient httpClient = new HttpClient();

                        byte[] imageBytes = await httpClient.GetByteArrayAsync(imageLink);

                        using Stream stream = new MemoryStream(imageBytes);

                        BitmapImage image = new BitmapImage();

                        image.BeginInit();
                        image.CacheOption = BitmapCacheOption.OnLoad;
                        image.StreamSource = stream;
                        image.EndInit();

                        Images.Add(image);
                    }
                    catch (Exception exception)
                    {
                        Logger.LogWarning(exception, "Не удалось загрузить фото серв. товара");
                        errorImageCount++;
                    }
                }
            }

            if (errorImageCount > 0)
            {
                MessageFacadeService.ShowNotificationWarning($"Не удалось загрузить {errorImageCount} фото из {imageLinks.Count}");
            }
        }

        private void CreateDiscountProduct()
        {
            DialogDocumentManagerService.ShowView<ServiceProductDiscountCreateViewModel>(parameter, this);
        }

        private void MapProductAttributes(ProductAttributesDto product)
        {
            ProductName = product.GetLocalName(LocalizableNameType.Ukr);
        }

        private async Task PrintBarcodeAsync(int productId, string productName)
        {
            BarcodeReportFactoryResult result = await BarcodeReportFactory.CreateAsync(productName, productId, 1);

            if (result.Printer == null)
            {
                MessageFacadeService.ShowNotificationError("Сначала задайте принтеры в настройках");
                return;
            }

            PrintReportRequest printReportRequest = new PrintReportRequest(
                result.Report,
                false,
                result.Printer.Name,
                result.Printer.PaperSource);

            await Mediator.Send(printReportRequest);
        }

        private void OnDiscountProductMessage(DiscountProductMessage message)
        {
            if (message.MessageType == MessageType.Added)
            {
                DiscountProducts.Insert(0, message.Entity);
            }
        }
    }
}