using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using MediatR;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.ReportFactory;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.Data.Requests.Features.AdditionalService;
using Telemart.Client.Data.Requests.Features.AdditionalServiceProduct;
using Telemart.Client.Data.Requests.Features.AdditionalServiceProduct.Actions;
using Telemart.Client.Data.Requests.Features.Call;
using Telemart.Client.Data.Requests.Features.Order;
using Telemart.Client.Data.Requests.Features.Products;
using Telemart.Client.Data.Requests.Features.ServiceProduct;
using Telemart.Client.Data.Requests.Features.ServiceProduct.Actions;
using Telemart.Client.Data.Requests.Features.ServiceRequest.Actions;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Mediator.Requests;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Call;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.TransferObjects.ServiceProduct;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Common.PrintBarcodeParameters;
using Telemart.Client.ViewModels.Service.ServiceProducts;
using Telemart.Client.ViewModels.Store.Call;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;
using static Telemart.Client.Data.Requests.Features.AdditionalServiceProduct.Actions.DefectAdditionalServiceProduct;

namespace Telemart.Client.ViewModels.AdditionalServiceProduct
{
    public sealed class AdditionalServiceProductDefectViewModel : ProductDefectViewModelBase<AdditionalServiceProductDefectResultDto, DefectAdditionalServiceProductDto>
    {
        private AdditionalServiceProductDefectParameter parameter;
        private int? _discountProductId;

        private ProductDiscountCreateDto _productDiscountCreateDto;
        private OrderDto _order;
        private volatile bool _createCall;

        public AdditionalServiceProductDefectViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMessenger messenger,
            IErrorHandler errorHandler,
            IBarcodeReportFactory barcodeReportFactory,
            IMediator mediator)
            : base(webClient, dictionaries, messageFacadeService, "Создание дефекта")
        {
            Messenger = messenger;
            ErrorHandler = errorHandler;
            BarcodeReportFactory = barcodeReportFactory;
            Mediator = mediator;
        }

        private IMessenger Messenger { get; }

        private IErrorHandler ErrorHandler { get; }

        private IBarcodeReportFactory BarcodeReportFactory { get; }

        private IMediator Mediator { get; }

        protected override CallEntityActionWithBodyRequestResultBase<AdditionalServiceProductDefectResultDto, DefectAdditionalServiceProductDto> GetDefectRequest()
        {
            return new DefectAdditionalServiceProduct(parameter.Id, StatedDefect, CreateDiscount, _createCall);
        }

        protected override ProductComboBoxItem? GetSelectedProduct()
        {
            return new ProductComboBoxItem(1, parameter.ProductName, parameter.KeepSerial);
        }

        protected override string GetSerialNumber()
        {
            return parameter.SerialNumber;
        }

        protected override async Task<bool> BeforeProcessOkAsync()
        {
            Task<OrderDto> orderTask = WebClient.ExecuteApiRequestAsync(new QueryOrder(parameter.OrderId));

            Task<AdditionalServiceProductDto> additionalServiceProductTask = WebClient.ExecuteApiRequestAsync(new QueryAdditionalServiceProduct(parameter.Id));

            await Task.WhenAll(orderTask, additionalServiceProductTask);

            _order = orderTask.Result;

            AdditionalServiceProductDto additionalServiceProduct = additionalServiceProductTask.Result;

            IReadOnlyCollection<ValidationResultItem> errors = CanCreateDefect(_order, additionalServiceProduct)?.ToArray();

            if (errors?.Any() == true)
            {
                MessageFacadeService.ShowValidationResultView("Ошибки создания дефекта", errors.ToArray(), this);

                return false;
            }

            if (!CreateDiscount)
            {
                List<int> callStates = new List<int>();
                callStates.Add(CallState.NewId);

                PagedResult<CallDto> calls = await WebClient.ExecuteApiRequestAsync(new QueryCalls(new CallFilteringItem()
                {
                    OrderIds = _order.Id.ToString(),
                    CallStates = callStates
                }));

                _createCall = calls?.Data?.Any() != true || MessageFacadeService.Confirm("Уже создан один звонок в статусе Новый, вы хотите создать еще один?");

                return true;
            }

            ServiceProductDiscountParameter discountParameter = new ServiceProductDiscountParameter(
                0,
                additionalServiceProduct.ProductId,
                parameter.SerialNumber,
                parameter.WarehouseId,
                ServiceProductType.Discount.Id,
                null,
                true);

            ServiceProductDiscountCreateViewModel model = DialogDocumentManagerService.ShowView<ServiceProductDiscountCreateViewModel>(discountParameter, this);

            if (model.IsOk)
            {
                _productDiscountCreateDto = model.GetProductDiscount();

                return true;
            }

            return false;
        }

        protected override async Task AfterSuccessOkAsync(AdditionalServiceProductDefectResultDto result)
        {
            if (CreateDiscount)
            {
                Result<ServiceRequestDto> fastCompleteResult = await ErrorHandler.HandleErrorsAsync(
                    _ => FastCompleteServiceRequestAsync(result.ServiceRequest, result.AdditionalServiceProduct.WarehouseId),
                    "быстром завершении СЗ",
                    "Быстрое завершение СЗ",
                    this,
                    true);

                if (fastCompleteResult?.IsSuccess != true || fastCompleteResult.Data?.ServiceProductIds?.Any() != true)
                {
                    return;
                }

                bool success = await ErrorHandler.HandleErrorsAsync(
                    _ => ServiceProductProccessAsync(fastCompleteResult.Data.ServiceProductIds.First()),
                    "создании уценки",
                    "Создание уценки",
                    this,
                    true);

                if (success)
                {
                    await FillSourcesInternalAsync(result);
                }
            }

            Messenger.Send(new EntityMessage<AdditionalServiceProductDto>(result.AdditionalServiceProduct, MessageType.Changed));
        }

        protected override async Task HandleLoadedAsync()
        {
            parameter = (AdditionalServiceProductDefectParameter)Parameter;

            AdditionalServiceDto additionalService = await WebClient.ExecuteApiRequestAsync(new QueryAdditionalService(parameter.AdditionalServiceId));

            VisibleCreateDiscount = additionalService.CreateDiscount;
            CreateDiscount = additionalService.CreateDiscount;

            await base.HandleLoadedAsync();
        }

        private async Task FillSourcesInternalAsync(AdditionalServiceProductDefectResultDto defect)
        {
            if (_discountProductId.HasValue)
            {
               Result result = await ErrorHandler.HandleErrorsAsync(
                    _ => WebClient.ExecuteApiRequestAsync(new SetUnavailableDiscountProduct(parameter.Id, _discountProductId.Value, defect.ServiceRequest.OrderId)),
                    "замене товара на уценку",
                    "Товар в заказе заменен на уценку",
                    this,
                    true);

               if (result?.IsSuccess == true && MessageFacadeService.Confirm("Распечатать новый ШК для уценочного товара?"))
               {
                   ProductCardDto dto = await WebClient.ExecuteApiRequestAsync(new QueryProductCard(_discountProductId.Value));

                   await PrintBarcodeAsync(dto.ProductId, dto.GetLocalName(LocalizableNameType.Ukr));
               }
            }
        }

        private async Task<Result<ServiceRequestDto>> FastCompleteServiceRequestAsync(ServiceRequestDto serviceRequest, int warehouseId)
        {
            try
            {
                LockResponse<ServiceRequestDto> lockResponce = await WebClient.ExecuteApiRequestAsync(new LockServiceRequest(serviceRequest.Id));

                if (lockResponce?.Success == true)
                {
                    FastCompleteServiceRequestProccesingDto requestDto = new FastCompleteServiceRequestProccesingDto
                    {
                        ServiceRequestId = serviceRequest.Id,
                        Appearance = ServiceRequestAppearance.LooksLikePresale.Name,
                        Inspection = ServiceRequestInspection.DefectConfirmed.Name,
                        SerialNumber = SerialNumber,
                        AdditionalServiceWarehouseId = warehouseId
                    };

                    Result<ServiceRequestDto> fastResult = await WebClient.ExecuteApiRequestAsync(new FastCompleteServiceRequest(requestDto));

                    return fastResult;
                }

                return Result<ServiceRequestDto>.Error("Ошибка при блокировке сервисной заявки", lockResponce?.Message);
            }
            finally
            {
                await WebClient.ExecuteApiRequestAsync(new UnlockServiceRequest(serviceRequest.Id));
            }
        }

        private async Task<bool> ServiceProductProccessAsync(int serviceProductId)
        {
            try
            {
                LockResponse<ServiceProductDto> lockResponse = await WebClient.ExecuteApiRequestAsync(new LockServiceProduct(serviceProductId));

                if (lockResponse?.Success != true)
                {
                    return false;
                }

                Result<ProductCardDto> result = await ErrorHandler.HandleErrorsAsync(
                    _ => WebClient.ExecuteApiRequestAsync(new CreateProductDiscount(lockResponse.Dto.ProductId, _productDiscountCreateDto)),
                    "создании уцененного товара",
                    "Уцененный товар создан",
                    this,
                    false);

                if (result.IsSuccess)
                {
                    DiscountServiceProduct gatewayRequest = new DiscountServiceProduct(lockResponse.Dto.Id, lockResponse.Dto.Sn, lockResponse.Dto.WarehouseId, result.Data.ProductId, true);

                    Result<ServiceProductDto> resultDiscountServiceProduct = await ErrorHandler.HandleErrorsAsync(
                        _ => WebClient.ExecuteApiRequestAsync(gatewayRequest),
                        "создании уценки",
                        "Уценка создана",
                        this,
                        false);

                    _discountProductId = resultDiscountServiceProduct.IsSuccess ? resultDiscountServiceProduct.Data.ProductDiscountId : null;

                    return _discountProductId.HasValue;
                }

                return false;
            }
            finally
            {
               await WebClient.ExecuteApiRequestAsync(new UnlockServiceProduct(serviceProductId));
            }
        }

        private IEnumerable<ValidationResultItem> CanCreateDefect(OrderDto orderDto, AdditionalServiceProductDto additionalServiceProduct)
        {
            if (orderDto.StateId != OrderStatus.Received.Id)
            {
                yield return new ValidationResultItem("Заказ должен быть в статусе \"Принят\"", true);
            }

            if (additionalServiceProduct.StateId != AdditionalServiceProductState.Warehouse.Id)
            {
                yield return new ValidationResultItem("Оказание услуги должно быть в статусе \"На складе\"", true);
            }
        }

        private async Task PrintBarcodeAsync(int productId, string name)
        {
            PrintBarcodeParametersParameter barcodeParameter = new PrintBarcodeParametersParameter(1, 1);

            PrintBarcodeParametersViewModel viewModel = DialogDocumentManagerService.ShowView<PrintBarcodeParametersViewModel>(barcodeParameter, this);

            if (viewModel.IsOk)
            {
                short copies = (short)viewModel.Count;
                int quantity = (int)viewModel.Quantity;

                BarcodeReportFactoryResult result = await BarcodeReportFactory.CreateAsync(viewModel.Format, name, productId, quantity);

                if (result.Printer == null)
                {
                    MessageFacadeService.ShowNotificationError("Сначала задайте принтеры в настройках");
                    return;
                }

                PrintReportRequest printReportRequest = new PrintReportRequest(
                    result.Report,
                    false,
                    result.Printer.Name,
                    result.Printer.PaperSource,
                    copies);

                await Mediator.Send(printReportRequest);
            }
        }
    }
}