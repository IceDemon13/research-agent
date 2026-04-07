using System.Threading.Tasks;
using DevExpress.Mvvm.DataAnnotations;
using MediatR;
using Telemart.Client.Business;
using Telemart.Client.Business.Order;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.ReportFactory;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Products;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Mediator.Requests;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Common
{
    public sealed class PrintOurBarcodeViewModel : TelemartDialogViewModelBase
    {
        private readonly IBarcodeReportFactory _barcodeReportFactory;
        private readonly IMediator _mediator;
        private readonly IOrderRules _orderRules;
        private readonly IErrorHandler _errorHandler;

        public PrintOurBarcodeViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IBarcodeReportFactory barcodeReportFactory,
            IMediator mediator,
            IOrderRules orderRules,
            IErrorHandler errorHandler)
            : base(webClient, dictionaries, messageFacadeService)
        {
            _barcodeReportFactory = barcodeReportFactory;
            _mediator = mediator;
            _orderRules = orderRules;
            _errorHandler = errorHandler;
        }

        public string OurBarcode
        {
            get { return GetProperty(() => OurBarcode); }
            set { SetProperty(() => OurBarcode, value); }
        }

        public int Quantity
        {
            get { return GetProperty(() => Quantity); }
            set { SetProperty(() => Quantity, value); }
        }

        public static void BuildMetadata(MetadataBuilder<PrintOurBarcodeViewModel> builder)
        {
            builder.Property(x => x.Quantity)
                .MatchesRule(x => x is > 0 and <= 100, () => "Кол-во должно быть в диапазоне 1..100");

            builder.Property(x => x.OurBarcode)
                .MatchesInstanceRule((x, y) => !string.IsNullOrWhiteSpace(x) && x.Length < 13 && y._orderRules.IsOurBarcode(x), (x, y) => y._orderRules.ValidateOurBarcode(x));
        }

        protected override Task HandleLoadedAsync()
        {
            Quantity = 1;

            Title = "Печать нашего ШК";

            return base.HandleLoadedAsync();
        }

        protected override async Task HandleOkAsync()
        {
            ProductCardDto productCard = await GetProductCartAsync();

            if (productCard == null)
            {
                MessageFacadeService.ShowNotificationError("Не найден товар с таким ШК");

                return;
            }

            BarcodeReportFactoryResult result = await _barcodeReportFactory.CreateAsync(productCard.NameUkr ?? productCard.Name, productCard.ProductId, 1);

            if (string.IsNullOrEmpty(result.Printer?.Name))
            {
                MessageFacadeService.ShowNotificationError("Для печати \"Нашего ШК\" нужно задать принтер в настройках");
                return;
            }

            PrintReportRequest printReportRequest = new PrintReportRequest(
                result.Report,
                false,
                result.Printer.Name,
                result.Printer.PaperSource,
                (short)Quantity);

            await _mediator.Send(printReportRequest);

            OurBarcode = null;
        }

        public async Task<ProductCardDto> GetProductCartAsync()
        {
            ProductBarcode productBarcode = new ProductBarcode(OurBarcode);

            ProductCardDto productCardDto = await _errorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new QueryProductCard(productBarcode.OurProductId!.Value)),
                null,
                null,
                this,
                false,
                showDialog: false,
                showError: false,
                showNotification: false);

            return productCardDto;
        }
    }
}