using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Core;
using DevExpress.XtraReports;
using MediatR;
using Microsoft.Extensions.Logging;
using Telemart.Client.Business;
using Telemart.Client.Common;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Settings.Printing;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.Requests.Features.Order;
using Telemart.Client.Data.Requests.Features.PackList;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Helpers;
using Telemart.Client.Mediator.Requests;
using Telemart.Client.Properties;
using Telemart.Client.Reports.Order;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.PackList;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Common;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Store.Order.PackList
{
    public sealed class PackListViewModel : TelemartDialogViewModelBase
    {
        private IReadOnlyDictionary<int, string> _employees;
        private IReadOnlyCollection<KeyValuePair<int, int>> _simpleProductIdsOrderIds;
        private Dictionary<int, int> _additionalServiceProductIdsOrderIds;
        private Dictionary<int, int> _assemblyServiceIdsOrderIds;

        private Dictionary<int, int> _packedSimpleProductIdsOrderIds;
        private Dictionary<int, int> _packedAdditionalServiceProductIdsOrderIds;
        private Dictionary<int, int> _packedAssemblyServiceIdsOrderIds;

        private int _warehouseId;

        public PackListViewModel(
            IMapper mapper,
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IPrintingSettingsStore printingSettingsStore,
            IMediator mediator,
            IErrorHandler errorHandler,
            IMessenger messanger,
            ILockableOperationProcessorFactory lockableOperationProcessorFactory)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            PrintingSettingsStore = printingSettingsStore;
            Mediator = mediator;
            ErrorHandler = errorHandler;
            Messenger = messanger;
            LockableOperationProcessorFactory = lockableOperationProcessorFactory;

            PrintCommand = new AsyncCommand(PrintAsync);
            PackTransferCommand = new AsyncCommand(
                PackTransferAsync,
                () => PackList != null && PackList?.CollectedBy == null
                                       && WebClient.IsOperationAllowed(BusinessOperation.PackListTransferAllow));
            DeleteOrderCommand = new AsyncCommand<PackListOrderViewItem>(DeleteOrderAsync, x => x != null);

            RecognizeBarcodeViewModel = new RecognizeBarcodeViewModel(webClient, dictionaries, messageFacadeService);
            RecognizeBarcodeViewModel.UseQuantity = false;
            RecognizeBarcodeViewModel.OnStarted += RecognizeBarcodeViewModelOnStarted;
            RecognizeBarcodeViewModel.OnFinished += RecognizeBarcodeViewModelOnFinished;

            _packedSimpleProductIdsOrderIds = new Dictionary<int, int>();
            _packedAdditionalServiceProductIdsOrderIds = new Dictionary<int, int>();
            _packedAssemblyServiceIdsOrderIds = new Dictionary<int, int>();
        }

        public PackListViewModel()
        {
        }

        public IAsyncCommand PrintCommand { get; }

        public IAsyncCommand PackTransferCommand { get; }

        public IAsyncCommand DeleteOrderCommand { get; }

        public PackListViewItem PackList
        {
            get { return GetProperty(() => PackList); }
            private set { SetProperty(() => PackList, value); }
        }

        public ReadOnlyObservableCollection<OrderStatus> States
        {
            get { return GetProperty(() => States); }
            private set { SetProperty(() => States, value); }
        }

        public IEnumerable<SummaryViewItem> SummaryItems
        {
            get { return GetProperty(() => SummaryItems); }
            private set { SetProperty(() => SummaryItems, value); }
        }

        public bool IsRecognitionInProgress
        {
            get { return GetProperty(() => IsRecognitionInProgress); }
            private set { SetProperty(() => IsRecognitionInProgress, value); }
        }

        public RecognizeBarcodeViewModel RecognizeBarcodeViewModel { get; }

        private IMapper Mapper { get; }

        private IMediator Mediator { get; }

        private IErrorHandler ErrorHandler { get; }

        private IPrintingSettingsStore PrintingSettingsStore { get; }

        private IMessenger Messenger { get; }

        private ILockableOperationProcessorFactory LockableOperationProcessorFactory { get; }

        protected override async Task HandleLoadedAsync()
        {
            PackListParameter parameter = (PackListParameter)Parameter;

            _warehouseId = parameter.WarehouseId;

            States = Dictionaries.GetItems<OrderStatus>().ToReadOnlyObservableCollection();

            Task<PackListDto> packListDtoTask = WebClient.ExecuteApiRequestAsync(new QueryPackList(parameter.Id));

            Task<PagedResult<EmployeeDto>> employeesResultTask = WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true);

            (PackListDto packListDto, PagedResult<EmployeeDto> employees) result = await TaskExt.WhenAll(packListDtoTask, employeesResultTask);

            _employees = result.employees.Data.ToDictionary(x => x.Id, y => y.Name);

            PackList = Mapper.Map<PackListViewItem>(result.packListDto);

            SummaryItems = GetSummaryItems();

            if (PackList.RecognizeBarcodeViewModelVisible)
            {
                RecognizeBarcodeViewModel.Init(
                    new RecognizeBarcodeSettings(
                        allowEan13: true,
                        true,
                        allowCode39: true,
                        allowOurAssemblyService: true,
                        additionalServiceProductIds: result.packListDto.PackListOrders
                            .Where(x => x.AdditionalServiceProductIds is not null)
                            .SelectMany(x => x.AdditionalServiceProductIds)
                            .ToArray()),
                    null);

                _simpleProductIdsOrderIds = result.packListDto.PackListOrders
                    .Where(x => x.SimpleProductIds is not null && x.OrderStateId != OrderStatus.Packed.Id)
                    .SelectMany(x => x.SimpleProductIds, (x, y) => new KeyValuePair<int, int>(y, x.OrderId))
                    .ToList();

                _assemblyServiceIdsOrderIds = result.packListDto.PackListOrders
                    .Where(x => x.AssemblyServiceIds is not null && x.OrderStateId != OrderStatus.Packed.Id)
                    .SelectMany(x => x.AssemblyServiceIds, (x, y) => new KeyValuePair<int, int>(y, x.OrderId))
                    .ToDictionary(x => x.Key, x => x.Value);

                _additionalServiceProductIdsOrderIds = result.packListDto.PackListOrders
                    .Where(x => x.AdditionalServiceProductIds is not null && x.OrderStateId != OrderStatus.Packed.Id)
                    .SelectMany(x => x.AdditionalServiceProductIds, (x, y) => new KeyValuePair<int, int>(y, x.OrderId))
                    .ToDictionary(x => x.Key, x => x.Value);

                _packedSimpleProductIdsOrderIds = result.packListDto.PackListOrders
                    .Where(x => x.SimpleProductIds is not null && x.OrderStateId == OrderStatus.Packed.Id)
                    .SelectMany(x => x.SimpleProductIds, (x, y) => new KeyValuePair<int, int>(y, x.OrderId))
                    .DistinctBy(x => x.Key)
                    .ToDictionary(x => x.Key, x => x.Value);

                _packedAssemblyServiceIdsOrderIds = result.packListDto.PackListOrders
                    .Where(x => x.AssemblyServiceIds is not null && x.OrderStateId == OrderStatus.Packed.Id)
                    .SelectMany(x => x.AssemblyServiceIds, (x, y) => new KeyValuePair<int, int>(y, x.OrderId))
                    .ToDictionary(x => x.Key, x => x.Value);

                _packedAdditionalServiceProductIdsOrderIds = result.packListDto.PackListOrders
                    .Where(x => x.AdditionalServiceProductIds is not null && x.OrderStateId == OrderStatus.Packed.Id)
                    .SelectMany(x => x.AdditionalServiceProductIds, (x, y) => new KeyValuePair<int, int>(y, x.OrderId))
                    .ToDictionary(x => x.Key, x => x.Value);
            }

            Title = $"Лист на сборку №{parameter.Id}";
        }

        protected override async Task HandleOkAsync()
        {
            try
            {
                Result<PackListDto> result = await WebClient.ExecuteApiRequestAsync(new CompletePackList(PackList.Id));

                if (result.Warnings.Any())
                {
                    MessageFacadeService.ShowNotificationWarning(
                        $"Лист на сборку №{PackList.Id} завершен с предупреждениями");
                    ShowValidationResultView(
                        "Предупрежедения",
                        result.Warnings.Select(x => new ValidationResultItem(x, false)).ToList());
                }
                else
                {
                    MessageFacadeService.ShowNotificationInfo($"Лист на сборку №{PackList.Id} успешно завершен");
                }

                IsOk = true;
                Close();
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при завершении листа на сборку");
                ShowValidationResultView("Ошибки при завершении листа на сборку", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to complete pack list");
                ShowValidationResultView(
                    Resources.ServerConnectError,
                    new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при завершении листа на сборку");
                Logger.LogError(exception, "Error while completing pack list");
            }
        }

        private async Task DeleteOrderAsync(PackListOrderViewItem item)
        {
            string message = PackList.PackListOrders.Count <= 1
                ? "Лист на сборку будет завершен автоматически, так как в нем не осталось заказов для упаковки. Продолжить?"
                : "Вы уверены?";

            if (!MessageFacadeService.Confirm(message))
            {
                return;
            }

            try
            {
                Result<PackListDto> result =
                    await WebClient.ExecuteApiRequestAsync(new DeleteOrderFromPackList(PackList.Id, item.Id));

                if (result.Warnings.Any())
                {
                    MessageFacadeService.ShowNotificationWarning($"Заказ {item.OrderId} удален с предупреждениями");
                    ShowValidationResultView(
                        "Предупрежедения",
                        result.Warnings.Select(x => new ValidationResultItem(x, false)).ToList());
                }
                else
                {
                    MessageFacadeService.ShowNotificationInfo(
                        $"Заказ {item.OrderId} успешно удален из листа на сборку");
                }

                if (result.Data.PackListOrders!.Any() != true)
                {
                    CloseOk();
                }

                PackList = Mapper.Map<PackListViewItem>(result.Data);

                SummaryItems = GetSummaryItems();
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при удалении заказа из листа на сборку");
                ShowValidationResultView("Ошибки при удалении заказа из листа на сборку", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to delete order from packlist");
                ShowValidationResultView(
                    Resources.ServerConnectError,
                    new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при удалении заказа из листа на сборку");
                Logger.LogError(exception, "Error while deleting order from packlist");
            }
        }

        private async Task PrintAsync()
        {
            try
            {
                PrintingSettingsInfo printingSettings = await PrintingSettingsStore.LoadAsync();

                IReadOnlyDictionary<int, Subdivision> subdivisions =
                    Dictionaries.GetItems<Subdivision>().ToDictionary(x => x.Id);

                PackListPrintDto printDto = await WebClient.ExecuteApiRequestAsync(new QueryPrintPackList(PackList.Id));

                if (printDto.Products?.Any() != true)
                {
                    MessageFacadeService.ShowNotificationError("Нельзя печатать лист на сборку в котором нет товаров");
                    return;
                }

                IReadOnlyCollection<OrderPackListProductReportData> products = printDto.Products
                    .Select(Mapper.Map<OrderPackListProductReportData>)
                    .ToArray();

                OrderPackListReportData reportData = new OrderPackListReportData(
                    PackList.Id,
                    products,
                    printDto.PackListOrders.Length,
                    subdivisions.GetValueOrDefault(printDto.SubdivisionId),
                    printDto.PackListOrders.Sum(x => x.Weight),
                    _employees.GetValueOrDefault(printDto.CreatedBy),
                    printDto.CreatedOn);

                reportData.SetDateTimeAssemblyPrint(DateTime.Now);

                IReport report = new OrderPackListReport { DataSource = new[] { reportData } };

                PrintReportRequest printReportRequest = new PrintReportRequest(
                    report,
                    true,
                    printingSettings.Main?.Name,
                    printingSettings.Main?.PaperSource);

                await Mediator.Send(printReportRequest);
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при формировании отчета");
                ShowValidationResultView("Ошибки при формировании отчета", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to create report");
                ShowValidationResultView(
                    Resources.ServerConnectError,
                    new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при формировании отчета");
                Logger.LogError(exception, "Error while creating report");
            }
        }

        private async Task PackTransferAsync()
        {
            int? selectedCollectorEmployeeId = PackList.CollectorEmployeeId;

            if (selectedCollectorEmployeeId is null)
            {
                PackListTransferPackViewModel model =
                    DialogDocumentManagerService.ShowView<PackListTransferPackViewModel>(
                        new PackListTransferPackParameter(PackList.Id, _warehouseId),
                        this);

                if (!model.IsOk)
                {
                    return;
                }

                selectedCollectorEmployeeId = model.SelectedCollectorEmployeeId;

                if (selectedCollectorEmployeeId is null)
                {
                    return;
                }
            }

            Result<PackListDto> result = await ErrorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(
                    new PackListTransferPack(PackList.Id, selectedCollectorEmployeeId.Value)),
                "передаче на упаковку",
                "Лист на сборку передан на упаковку",
                this,
                true);

            if (result.IsSuccess)
            {
                string[] ordersIds = result.Data.PackListOrders.Select(x => x.OrderId.ToString()).ToArray();

                OrderFilteringItem orderFilteringItem = new OrderFilteringItem(string.Empty, new List<int>())
                {
                    OrderNumbers = string.Join(", ", ordersIds),
                    PackListInfo = true
                };

                List<OrderDto> packOrders = await WebClient
                    .ExecuteApiRequestAsync(new QueryOrders(orderFilteringItem)).GetPagedResultDataAsync();

                foreach (OrderDto packOrder in packOrders)
                {
                    Messenger.Send(new OrderMessage(packOrder, MessageType.Changed));
                }

                await HandleLoadedCommand.ExecuteAsync(null);
            }
        }

        private IEnumerable<SummaryViewItem> GetSummaryItems()
        {
            yield return new SummaryViewItem("Номер", $"{PackList.Id}");
            yield return new SummaryViewItem("Создан", PackList.CreatedOn.ToString(DateFormattingRules.DateFormat));
            yield return new SummaryViewItem("Создал", _employees.GetValueOrDefault(PackList.CreatedBy));

            if (PackList.PackagerEmployeeId.HasValue)
            {
                yield return new SummaryViewItem(
                    "Упаковщик",
                    _employees.GetValueOrDefault(PackList.PackagerEmployeeId.Value));
            }

            if (PackList.CollectorEmployeeId.HasValue)
            {
                yield return new SummaryViewItem(
                    "Сборщик",
                    _employees.GetValueOrDefault(PackList.CollectorEmployeeId.Value));
            }

            if (PackList.CollectedBy.HasValue && PackList.CollectedOn.HasValue)
            {
                yield return new SummaryViewItem(
                    "Собран",
                    PackList.CollectedOn.Value.ToString(DateFormattingRules.DateFormat));
                yield return new SummaryViewItem("Собрал", _employees.GetValueOrDefault(PackList.CollectedBy.Value));
            }

            yield return new SummaryViewItem("Заказов", PackList.PackListOrders.Count.ToString());
            yield return new SummaryViewItem("Строк", PackList.PackListOrders.Sum(x => x.RowCount).ToString());
            yield return new SummaryViewItem("Товаров", PackList.PackListOrders.Sum(x => x.ProductCount).ToString());
            yield return new SummaryViewItem("Вес", PackList.PackListOrders.Sum(x => x.Weight).ToString("N1"));
        }

        private void RecognizeBarcodeViewModelOnStarted(object sender, EventArgs e)
        {
            IsRecognitionInProgress = true;
        }

        private async void RecognizeBarcodeViewModelOnFinished(object sender, RecognizeBarcodeResultEventArgs e)
        {
            SplashScreenManager splashScreenManager = null;

            try
            {
                splashScreenManager = SplashScreenManager.CreateWaitIndicator();
                splashScreenManager.Show();

                IsRecognitionInProgress = false;

                switch (e.Result)
                {
                    case RecognizeBarcodeResult.Found:
                    case RecognizeBarcodeResult.FoundInSupplier:

                        KeyValuePair<int, int> productIdOrderId =
                            _simpleProductIdsOrderIds.FirstOrDefault(x => x.Key == e.ProductId!.Value);

                        if (productIdOrderId.Key > 0)
                        {
                            await ShowOrderPackViewModelAsync(productIdOrderId.Value, e.BarcodeText!);
                        }
                        else
                        {
                            if (_packedSimpleProductIdsOrderIds.TryGetValue(e.ProductId!.Value, out int packedOrderId))
                            {
                                MessageFacadeService.ShowNotificationWarning(
                                    $"Товар уже упакован в заказ {packedOrderId}");
                            }
                            else
                            {
                                MessageFacadeService.ShowNotificationError("Товар не найден в листе на сборку");
                            }
                        }

                        break;
                    case RecognizeBarcodeResult.FoundAssembly:
                        if (_assemblyServiceIdsOrderIds.TryGetValue(e.AssemblyServiceId!.Value, out int orderId))
                        {
                            await ShowOrderPackViewModelAsync(orderId, e.BarcodeText!);
                        }
                        else
                        {
                            if (_packedAssemblyServiceIdsOrderIds.TryGetValue(
                                    e.AssemblyServiceId!.Value,
                                    out int packedOrderId))
                            {
                                MessageFacadeService.ShowNotificationWarning(
                                    $"Сборка уже упакована в заказ {packedOrderId}");
                            }
                            else
                            {
                                MessageFacadeService.ShowNotificationError("Сборка не найдена в листе на сборку");
                            }
                        }

                        break;

                    case RecognizeBarcodeResult.FoundAdditionalServiceProduct:

                        if (_additionalServiceProductIdsOrderIds.TryGetValue(
                                e.AdditionalServiceProductId!.Value,
                                out orderId))
                        {
                            await ShowOrderPackViewModelAsync(orderId, e.BarcodeText!);
                        }
                        else
                        {
                            if (_packedAdditionalServiceProductIdsOrderIds.TryGetValue(
                                    e.AdditionalServiceProductId!.Value,
                                    out int packedOrderId))
                            {
                                MessageFacadeService.ShowNotificationWarning(
                                    $"Услуга уже упакована в заказ {packedOrderId}");
                            }
                            else
                            {
                                MessageFacadeService.ShowNotificationError("Услуга не найдена в листе на сборку");
                            }
                        }

                        break;
                    case RecognizeBarcodeResult.NotFound:
                        e.Message = new RecognizeBarcodeMessage(
                            RecognizeBarcodeMessageType.Warning,
                            "ШК не найден в БД");
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to recognize barcode");
                MessageFacadeService.ShowNotificationError("Ошибка при распознавании ШК");
            }
            finally
            {
                splashScreenManager?.Close();
                RecognizeBarcodeViewModel.SetFocusOnBarcode();
            }
        }

        private async Task ShowOrderPackViewModelAsync(int orderId, string scannedBarcode)
        {
            Result<OrderPackInfoDto> result = await ErrorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new QueryOrderPackInfo(orderId)),
                "запросе информации об упаковке",
                null,
                this,
                true,
                showNotification: false);

            if (result?.IsSuccess != true)
            {
                return;
            }

            result.Data.ScannedBarcode = scannedBarcode;

            OrderPackViewModel orderPackModel = null;

            await LockableOperationProcessorFactory.Create<OrderDto>().DoActionAsync(
                orderId,
                x =>
                {
                    orderPackModel = SizeableDialogDocumentManagerService.ShowView<OrderPackViewModel>(result.Data, this);
                });

            if (orderPackModel?.IsOk != true)
            {
                return;
            }

            OrderDto orderDto = await WebClient.ExecuteApiRequestAsync(new QueryOrder(orderId));

            PackListOrderViewItem orderItem = PackList.PackListOrders.First(x => x.OrderId == orderId);

            orderItem.OrderState = Dictionaries.GetItemById<OrderStatus>(orderDto.StateId);

            if (orderItem.OrderState == OrderStatus.Packed)
            {
                IEnumerable<KeyValuePair<int, int>> orderSimpleProductIdsOrderIds = _simpleProductIdsOrderIds
                    .DistinctBy(x => x.Key)
                    .Where(x => x.Value == orderId && !_packedSimpleProductIdsOrderIds.ContainsKey(x.Key));

                _packedSimpleProductIdsOrderIds.AddRange(orderSimpleProductIdsOrderIds);
                _packedAssemblyServiceIdsOrderIds.AddRange(_assemblyServiceIdsOrderIds.Where(x => x.Value == orderId));
                _packedAdditionalServiceProductIdsOrderIds.AddRange(_additionalServiceProductIdsOrderIds.Where(x => x.Value == orderId));

                _simpleProductIdsOrderIds = _simpleProductIdsOrderIds.Where(x => x.Value != orderId).ToArray();
                _assemblyServiceIdsOrderIds = _assemblyServiceIdsOrderIds.Where(x => x.Value != orderId).ToDictionary(x => x.Key, x => x.Value);
                _additionalServiceProductIdsOrderIds = _additionalServiceProductIdsOrderIds.Where(x => x.Value != orderId).ToDictionary(x => x.Key, x => x.Value);

                if (PackList.PackListOrders.All(x => x.OrderState == OrderStatus.Packed))
                {
                    await HandleOkAsync();
                }
            }
        }
    }
}