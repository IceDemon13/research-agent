using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using MediatR;
using Microsoft.Extensions.Logging;
using Telemart.Client.Business;
using Telemart.Client.Common;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Core.IO;
using Telemart.Client.Data.Requests.Features.Cashbox;
using Telemart.Client.Data.Requests.Features.FiscalDocument;
using Telemart.Client.Data.Requests.Features.Order;
using Telemart.Client.Data.Requests.Features.Order.Actions;
using Telemart.Client.Data.Requests.Features.OrderBill;
using Telemart.Client.Data.Requests.Features.OrderDocuments;
using Telemart.Client.Data.Requests.Features.OrderPayment;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.FiscalRegistrar;
using Telemart.Client.FiscalRegistrar.Abstraction;
using Telemart.Client.Mediator.Requests;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Cashbox;
using Telemart.Client.ViewModels.Store.Order;
using Telemart.Client.ViewModels.Store.Order.OrderEditPrice;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;
using Telemart.Fiscal.Client.Extensions;
using Telemart.Fiscal.Client.TransferObjects;

namespace Telemart.Client.Helpers
{
    public class OrderGiveHelper : IOrderGiveHelper
    {
        private readonly IMessageFacadeService _messageFacadeService;
        private readonly IFiscalRegistrarClientFactory _fiscalRegistrarClientFactory;
        private readonly IMapper _mapper;
        private readonly IWebClient _webClient;
        private readonly IMessenger _messenger;
        private readonly IMediator _mediator;
        private readonly ILogger<OrderGiveHelper> _logger;
        private readonly IErrorHandler _errorHandler;
        private readonly LockableOperationProcessor<OrderDto> _lockableOperationProcessor;
        private readonly IRroPrintHelper _rroPrintHelper;
        private readonly IDictionaries _dictionaries;

        public OrderGiveHelper(
            IMessageFacadeService messageFacadeService,
            IWebClient webClient,
            IMessenger messenger,
            IMapper mapper,
            IMediator mediator,
            IFiscalRegistrarClientFactory fiscalRegistrarClientFactory,
            ILockableOperationProcessorFactory lockableOperationProcessorFactory,
            IErrorHandler errorHandler,
            IRroPrintHelper rroPrintHelper,
            ILogger<OrderGiveHelper> logger,
            IDictionaries dictionaries)
        {
            _webClient = webClient;
            _messageFacadeService = messageFacadeService;
            _messenger = messenger;
            _mapper = mapper;
            _mediator = mediator;
            _fiscalRegistrarClientFactory = fiscalRegistrarClientFactory;
            _errorHandler = errorHandler;
            _rroPrintHelper = rroPrintHelper;
            _logger = logger;
            _dictionaries = dictionaries;
            _lockableOperationProcessor = lockableOperationProcessorFactory.Create<OrderDto>();
        }

        public async Task<(bool success, OrderDto order)> GiveAsync(int orderId, string contractorName, string cityName, bool useCells, ISupportServices parent, bool? white = null, bool printWarrantyCard = true)
        {
            bool success = false;

            OrderDto order = null;

            IDocumentManagerService sizeableDialogDocumentManagerService = parent.ServiceContainer.GetService<IDocumentManagerService>("SizeableDialogDocumentManagerService", ServiceSearchMode.PreferParents);

            await _lockableOperationProcessor.DoOperationAsync(orderId, GiveInternalAsync, false);

            async Task GiveInternalAsync(OrderDto orderObj)
            {
                order = orderObj;

                Result<IFiscalRegistrarClient> fiscalClientResult = await _fiscalRegistrarClientFactory.CreateAsync(CancellationToken.None);

                IFiscalRegistrarClient fiscalRegistrarClient = fiscalClientResult.Data;

                OrderGiveInfoDto info = await GetOrderGiveInfoAsync(orderObj.Id, parent);

                order = info.Order;

                if (order.LegalEntity?.White == true && fiscalRegistrarClient == null && !_messageFacadeService.Confirm("Не заполнены настройки РРО. Выдать заказ?"))
                {
                    return;
                }

                int[] cellIds = null;

                if (useCells)
                {
                    List<OrderBillDto> orderBills = await _webClient.ExecuteApiRequestAsync(new QueryOrderBills(new QueryOrderBills.QueryOrderBillsFilteringItem(order.Id)));

                    int? billId = orderBills.Where(x => x.StateId == OrderBillState.Paid.Id)
                        .OrderByDescending(x => x.CreatedOn)
                        .Select(x => (int?)x.Id)
                        .FirstOrDefault();

                    List<OrderDocumentDto> orderDocuments = await _webClient.ExecuteApiRequestAsync(new QueryDocumentsByOrders(new[] { order.Id }, (int)OrderDocumentTypeIds.ActIncomeId));

                    OrderGiveCellViewModel orderGiveCellViewModel = sizeableDialogDocumentManagerService.ShowView<OrderGiveCellViewModel>(
                        new OrderGiveCellParameter(order, info.OrderCells, cityName, contractorName, "Выдача заказа", true, billId, orderDocuments?.Any() == true),
                        parent);

                    if (!orderGiveCellViewModel.IsOk)
                    {
                        return;
                    }

                    cellIds = orderGiveCellViewModel.OrderCells.Where(x => x.Completed).Select(x => x.CellId).ToArray();
                }
                else if (printWarrantyCard)
                {
                    int[] productIds = order.Products
                        .Where(x => x.Product.PrintWarrantyCard)
                        .Select(x => x.Product.Id)
                        .ToArray();

                    if (productIds.Any())
                    {
                        await _mediator.Send(new PrintWarrantyCardRequest(order.Id, productIds, null, true));
                    }
                }

                OrderPaymentDto orderPayment = null;

                if (order.Pko == 0)
                {
                    if ((order.LegalEntity?.White ?? white) != false)
                    {
                        (order, orderPayment) = await AddOrderPaymentViaCashRegistrarAsync(order, false, true, false, parent);
                    }
                    else
                    {
                        (order, orderPayment) = await AddOrderPaymentInternalAsync(order, true, false, parent);
                    }

                    if (orderPayment == null)
                    {
                        return;
                    }
                }
                else if (fiscalRegistrarClient != null && !_messageFacadeService.Confirm("Выдать заказ?"))
                {
                    return;
                }

                await TryGiveOrderAsync(orderPayment, cellIds, fiscalRegistrarClient);
            }

            async Task TryGiveOrderAsync(OrderPaymentDto orderPayment, int[] cellIds, IFiscalRegistrarClient fiscalRegistrarClient)
            {
                try
                {
                    bool printed = await PrintOrderChequeOnFiscalRegistrarAsync(order, orderPayment, fiscalRegistrarClient, parent);

                    if (!printed)
                    {
                        return;
                    }

                    Result<OrderDto> result = await _webClient.ExecuteApiRequestAsync(new GiveOrder(order.Id, cellIds));

                    if (result.Warnings.Any())
                    {
                        _messageFacadeService.ShowNotificationWarning($"Заказ №{order.Id.ToString(CultureInfo.InvariantCulture)} выдан с предупреждениями");
                        _messageFacadeService.ShowValidationResultView("Предупреждения", result.Warnings.Select(x => new ValidationResultItem(x, false)).ToList(), parent);
                    }
                    else
                    {
                        _messageFacadeService.ShowNotificationInfo($"Заказ №{order.Id.ToString(CultureInfo.InvariantCulture)} успешно выдан");
                    }

                    _messenger.Send(new OrderMessage(result.Data, MessageType.Changed));

                    order = result.Data;

                    success = true;
                }
                catch (UnexpectedSatusException exception)
                {
                    _messageFacadeService.ShowValidationResultView("Ошибки", exception.GetErrorItems(), parent);
                }
                catch (UnexpectedErrorException exception)
                {
                    _logger.LogError(exception, "Gailed to give order");
                    _messageFacadeService.ShowValidationResultView("Ошибки при получении данных", new[] { new ValidationResultItem(Resources.ServerUnavailable, true) }, parent);
                }
            }

            return (success, order);
        }

        public async Task<(bool success, OrderDto order)> FastGiveAsync(int orderId, string contractorName, string cityName, ISupportServices parent)
        {
            (bool Success, OrderDto Order) result = await GiveAsync(orderId, contractorName, cityName, false, parent);

            if (result.Success)
            {
                List<OrderBillDto> orderBills = await _webClient.ExecuteApiRequestAsync(new QueryOrderBills(new QueryOrderBills.QueryOrderBillsFilteringItem(result.Order.Id)));

                int? billId = orderBills.Where(x => x.StateId == OrderBillState.Paid.Id)
                    .OrderByDescending(x => x.CreatedOn)
                    .Select(x => (int?)x.Id)
                    .FirstOrDefault();

                if (billId.HasValue)
                {
                    await PrintBillAsync(billId.Value);

                    await PrintBillInvoiceAsync(billId.Value);
                }
            }

            return result;
        }

        public async Task<(OrderDto Order, OrderPaymentDto OrderPayment)> AddOrderPaymentViaCashRegistrarAsync(OrderDto order, bool printCheque, bool fullAmount, bool prepayment, ISupportServices parent)
        {
            IDocumentManagerService dialogDocumentManagerService = parent.ServiceContainer.GetService<IDocumentManagerService>("DialogDocumentManagerService", ServiceSearchMode.PreferParents);

            if (Payment.IsCreditPayment(order.PaymentId)
                && order.OrderPayments?.Any() != true
                && (!_webClient.IsOperationAllowed(BusinessOperation.OrderCreditPaymentIgnoreError)
                    && !_webClient.IsOperationAllowed(BusinessOperation.OrderCreditPaymentIgnoreErrorOnFirstPayment)))
            {
                _messageFacadeService.ShowNotificationWarning("В кредитных заказах, первая оплата не может быть внесена вручную");
                return default;
            }

            if (order.CompletedOnFiscalRegistrar && order.StateId == OrderStatus.Done.Id)
            {
                _messageFacadeService.ShowNotificationWarning("Запрещено вносить оплаты через РРО в выданные заказы");
                return default;
            }

            AddOrderPaymentViaCashRegistrarParameter parameter = new AddOrderPaymentViaCashRegistrarParameter(
                    _mapper.Map<OrderEditPriceViewItem>(order),
                    null,
                    fullAmount,
                    order.LegalEntity,
                    printCheque,
                    prepayment);

            AddOrderPaymentViaCashRegistrarViewModel vm = dialogDocumentManagerService.ShowView<AddOrderPaymentViaCashRegistrarViewModel>(parameter, parent);

            if (vm.IsOk)
            {
                if (printCheque && !string.IsNullOrEmpty(vm.FiscalId))
                {
                    Result resultSentRroCheck = await _rroPrintHelper.SentCheckAsync(vm.FiscalId, vm.Order.Phone, vm.Order.Email, vm.SellCashboxId, parent);

                    if (resultSentRroCheck.IsSuccess)
                    {
                        Result<OrderPaymentDto> result = await _webClient.ExecuteApiRequestAsync(new ConfirmOrderPaymentOnFiscalRegistrar(vm.OrderPayment.Id, vm.FiscalId, true, vm.SellCashboxId));

                        _messenger.Send(new OrderPaymentMessage(result.Data, MessageType.Changed));
                    }
                }

                return (vm.Order, vm.OrderPayment);
            }

            return default;
        }

        private async Task<(OrderDto Order, OrderPaymentDto OrderPayment)> AddOrderPaymentInternalAsync(OrderDto actualOrderObj, bool fullAmount, bool prepayment, ISupportServices parent)
        {
            IDocumentManagerService dialogDocumentManagerService = parent.ServiceContainer.GetService<IDocumentManagerService>("DialogDocumentManagerService", ServiceSearchMode.PreferParents);

            AddOrderPaymentParameter parameter = new AddOrderPaymentParameter(
                actualOrderObj.CreatedOn.Date,
                actualOrderObj.LegalEntity,
                actualOrderObj.GetPrices().ToPay,
                actualOrderObj.Products.Select(x => x.CurrencyOutId).Distinct().Select(Currency.GetById).ToArray(),
                actualOrderObj.ClientId,
                fullAmount,
                actualOrderObj.PaymentId);

            AddOrderPaymentViewModel vm = dialogDocumentManagerService.ShowView<AddOrderPaymentViewModel>(parameter, parent);

            if (vm.IsOk)
            {
                try
                {
                    CreateOrderPayment gatewayRequest = new CreateOrderPayment(
                        actualOrderObj.Id,
                        vm.SelectedCurrency.Id,
                        vm.SelectedPaymentType?.Id,
                        vm.SelectedCashboxId!.Value,
                        vm.Amount,
                        vm.ReceivedOn,
                        vm.Comment,
                        prepayment: prepayment);

                    Result<OrderPaymentResultDto> result = await _webClient.ExecuteApiRequestAsync(gatewayRequest);

                    _messageFacadeService.ShowNotificationInfo($"Данные об оплате для заказа №{result.Data.Order.Id} успешно сохранены");

                    _messenger.Send(new OrderPaymentMessage(result.Data.OrderPayment, MessageType.Added));

                    return (result.Data.Order, result.Data.OrderPayment);
                }
                catch (UnexpectedSatusException exception)
                {
                   _messageFacadeService.ShowValidationResultView("Ошибки при внесении оплаты заказа", exception.GetErrorItems(), parent);
                }
                catch (UnexpectedErrorException exception)
                {
                    _logger.LogError(exception, "Gailed to push orders payment");
                    _messageFacadeService.ShowValidationResultView("Ошибки при внесении оплаты заказа", new[] { new ValidationResultItem(Resources.ServerUnavailable, true) }, parent);
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Error while creating order payment");
                    _messageFacadeService.ShowNotificationError("Ошибка при сохранении");
                }
            }

            return default;
        }

        private async Task<OrderGiveInfoDto> GetOrderGiveInfoAsync(int orderId, ISupportServices parent)
        {
            OrderGiveInfoDto orderGiveInfo = null;

            try
            {
                Result<OrderGiveInfoDto> result = await _webClient.ExecuteApiRequestAsync(new QueryOrderGiveInfo(orderId));

                if (result.Warnings.Any())
                {
                    _messageFacadeService.ShowValidationResultView(
                        "Предупреждения",
                        result.Warnings.Select(x => new ValidationResultItem(x, false)).ToList(),
                        parent);

                    orderGiveInfo = result.Data;
                }
                else
                {
                    orderGiveInfo = result.Data;
                }
            }
            catch (UnexpectedSatusException exception)
            {
                _messageFacadeService.ShowValidationResultView("Ошибки", exception.GetErrorItems(), parent);
            }
            catch (UnexpectedErrorException exception)
            {
                _logger.LogError(exception, "Failed to give order");
                _messageFacadeService.ShowValidationResultView("Ошибки при получении данных", new[] { new ValidationResultItem(Resources.ServerUnavailable, true) }, parent);
            }

            return orderGiveInfo;
        }

        private async Task<bool> PrintOrderChequeOnFiscalRegistrarAsync(OrderDto order, OrderPaymentDto orderPayment, IFiscalRegistrarClient fiscalRegistrarClient, ISupportServices parent)
        {
            Payment payment = _dictionaries.GetItemById<Payment>(order.PaymentId);

            if (order.CompletedOnFiscalRegistrar || order.LegalEntity?.White == false || payment.Fiscal != true)
            {
                return true;
            }

            if (fiscalRegistrarClient == null)
            {
                _messageFacadeService.ShowNotificationWarning("Не заполнены настройки РРО");
                return false;
            }

            if (order.LegalEntity != null && order.LegalEntity.Id != fiscalRegistrarClient.Settings.LegalEntityId)
            {
                _messageFacadeService.ShowNotificationWarning("Юр. лицо заказа не соответствует юр. лицу выбранного РРО");
                return false;
            }

            CashboxDto cashbox = await _webClient.ExecuteApiRequestAsync(new QueryCashbox(fiscalRegistrarClient.Settings.CashboxId));

            Result<SellRequest> sellRequestResult = null;

            try
            {
                sellRequestResult = await _webClient.ExecuteApiRequestAsync(
                    new CreateFiscalDocumentSellRequest(
                        new CreateFiscalDocumentSellDto()
                        {
                            EmployeeId = _webClient.AuthenticatedEmployee.Id,
                            CashboxId = cashbox.Id,
                            OrderId = order.Id
                        }));
            }
            catch (UnexpectedSatusException)
            {
                await ConfirmFiscalRegistrarAsync(
                    order,
                    string.Empty,
                    true,
                    orderPayment?.Id,
                    cashbox.Id);
            }
            catch (UnexpectedErrorException exception)
            {
                _logger.LogError(exception, "Gailed to give order");
                _messageFacadeService.ShowValidationResultView("Ошибки при получении данных", new[] { new ValidationResultItem(Resources.ServerUnavailable, true) }, parent);
            }

            if (sellRequestResult == null)
            {
                return true;
            }

            if (sellRequestResult.IsSuccess != true)
            {
                await ConfirmFiscalRegistrarAsync(
                    order,
                    string.Empty,
                    true,
                    orderPayment?.Id,
                    cashbox.Id);
            }

            if (sellRequestResult.Data is null)
            {
                return true;
            }

            await ConfirmFiscalRegistrarAsync(
                order,
                sellRequestResult.Data.OperationId,
                false,
                orderPayment?.Id,
                sellRequestResult.Data.CashboxId);

            Result<SellResponse> sellResult = await _errorHandler.HandleErrorsAsync(
                ct => fiscalRegistrarClient.SellAsync(sellRequestResult.Data, ct),
                "проведении оплаты через РРО",
                "Оплата в РРО проведена",
                parent,
                true);

            Result<QueryReceiptStatusResponse> statusResult = await _errorHandler.HandleErrorsAsync(
                ct => fiscalRegistrarClient.GetReceiptStatusAsync(cashbox.Id, sellRequestResult.Data.OperationId, ct),
                "проведении оплаты через РРО",
                "Оплата в РРО проведена",
                parent,
                true);

            if (statusResult == null || statusResult.Data.Status == "ERROR")
            {
                await ConfirmFiscalRegistrarAsync(
                    order,
                    null,
                    false,
                    orderPayment?.Id,
                    sellRequestResult.Data.CashboxId);

                return false;
            }

            await _webClient.ExecuteApiRequestAsync(new CreateFiscalDocument(sellRequestResult.Data.ToDocument(cashbox.Id, sellRequestResult.Data.OperationId, order.Id, Entity.OrderId)));

            Result resultSentRroCheck = await _rroPrintHelper.SentCheckAsync(sellRequestResult.Data.OperationId, order.Phone, order.Email, sellRequestResult.Data.CashboxId, parent);

            if (resultSentRroCheck.IsSuccess)
            {
                await ConfirmFiscalRegistrarAsync(
                    order,
                    sellRequestResult.Data.OperationId,
                    true,
                    orderPayment?.Id,
                    sellRequestResult.Data.CashboxId);
            }

            return true;
        }

        private async Task ConfirmFiscalRegistrarAsync(OrderDto order, string fiscalId, bool isPrinted, int? orderPaymentId, int cashboxId)
        {
            await _webClient.ExecuteApiRequestAsync(new ConfirmOrderOnFiscalRegistrar(order.Id, fiscalId, isPrinted));

            if (orderPaymentId.HasValue)
            {
                await _webClient.ExecuteApiRequestAsync(new ConfirmOrderPaymentOnFiscalRegistrar(orderPaymentId.Value, fiscalId, isPrinted, cashboxId));
            }
        }

        private async Task PrintBillAsync(int billId)
        {
            try
            {
                byte[] document = await _webClient.ExecuteApiRequestAsBytesAsync(new ExportOrderBill(billId));
                await FileHelper.OpenAsFileAsync(document, Constants.XlsFileExtension);
            }
            catch (Exception ex)
            {
                _messageFacadeService.ShowNotificationWarning("Ошибка при печати счета");
                _logger.LogError(ex, "Failed to print bill");
            }
        }

        private async Task PrintBillInvoiceAsync(int billId)
        {
            try
            {
                byte[] document = await _webClient.ExecuteApiRequestAsBytesAsync(new ExportOrderBillInvoice(billId));
                await FileHelper.OpenAsFileAsync(document, Constants.XlsFileExtension);
            }
            catch (Exception ex)
            {
                _messageFacadeService.ShowNotificationWarning("Ошибка при печати расходной накладной");
                _logger.LogError(ex, "Failed to print bill invoice");
            }
        }
    }
}