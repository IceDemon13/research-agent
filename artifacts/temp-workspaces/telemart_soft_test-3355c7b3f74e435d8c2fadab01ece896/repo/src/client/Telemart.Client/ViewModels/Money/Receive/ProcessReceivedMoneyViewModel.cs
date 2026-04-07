using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Telemart.Client.Business;
using Telemart.Client.Common;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Utils.Import;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Core.Helpers;
using Telemart.Client.Data.Requests.Features.Cashbox;
using Telemart.Client.Data.Requests.Features.Order;
using Telemart.Client.Data.Requests.Features.OrderPayment;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Helpers;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Cashbox;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Parser.Dictionary;
using Telemart.Client.ViewModels.Store;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Money.Receive
{
    public sealed class ProcessReceivedMoneyViewModel : TelemartDialogViewModelBase
    {
        private List<CashboxDto> cashboxes;

        public ProcessReceivedMoneyViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMessenger messenger,
            IExcelImportEngine<ReceiveViewItem> excelImportEngine,
            UkrposhtaReceiveMoneyExcelImportEngine ukrposhtaExcelImportEngine,
            MeestReceiveMoneyExcelImportEngine meestReceiveMoneyExcelImportEngine)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Messenger = messenger;
            ExcelImportEngine = excelImportEngine;
            UkrposhtaExcelImportEngine = ukrposhtaExcelImportEngine;
            MeestTtnExcelImportEngine = meestReceiveMoneyExcelImportEngine;

            RemoveItemCommand = new DelegateCommand<ReceiveViewItem>(RemoveItem, x => x != null);
            ShowRecognizeDialogCommand = new AsyncCommand<MoneyReceiveRecognizeType>(ShowRecognizeDialogAsync);
            RemoveAllCommand = new DelegateCommand(RemoveAll);
        }

        public ProcessReceivedMoneyViewModel()
        {
        }

        #region Commands

        public IDelegateCommand RemoveAllCommand { get; }

        public IDelegateCommand RemoveItemCommand { get; }

        public IAsyncCommand ShowRecognizeDialogCommand { get; }

        #endregion

        #region INPC

        public ObservableCollection<ReceiveViewMessageItem> Messages
        {
            get { return GetProperty(() => Messages); }
            private set { SetProperty(() => Messages, value); }
        }

        public ReadOnlyObservableCollection<MoneyReceiveRecognizeType> RecognizeTypes
        {
            get { return GetProperty(() => RecognizeTypes); }
            private set { SetProperty(() => RecognizeTypes, value); }
        }

        public ReceiveViewMessageItem CurrentMessage
        {
            get { return GetProperty(() => CurrentMessage); }
            set { SetProperty(() => CurrentMessage, value); }
        }

        public IEnumerable<SummaryViewItem> SummaryItems
        {
            get { return GetProperty(() => SummaryItems); }
            private set { SetProperty(() => SummaryItems, value); }
        }

        public ObservableCollection<ReceiveViewItem> Items
        {
            get { return GetProperty(() => Items); }
            private set { SetProperty(() => Items, value); }
        }

        public ReceiveViewItem CurrentItem
        {
            get { return GetProperty(() => CurrentItem); }
            set { SetProperty(() => CurrentItem, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Cashboxes
        {
            get { return GetProperty(() => Cashboxes); }
            private set { SetProperty(() => Cashboxes, value); }
        }

        public int? SelectedCashboxId
        {
            get { return GetProperty(() => SelectedCashboxId); }
            set { SetProperty(() => SelectedCashboxId, value); }
        }

        public DateTime? ReceivedOn
        {
            get { return GetProperty(() => ReceivedOn); }
            set { SetProperty(() => ReceivedOn, value); }
        }

        public DateTime ReceivedOnMinValue
        {
            get { return GetProperty(() => ReceivedOnMinValue); }
            private set { SetProperty(() => ReceivedOnMinValue, value); }
        }

        public DateTime ReceivedOnMaxValue
        {
            get { return GetProperty(() => ReceivedOnMaxValue); }
            private set { SetProperty(() => ReceivedOnMaxValue, value); }
        }

        public Payment SelectedPayment
        {
            get { return GetProperty(() => SelectedPayment); }
            set { SetProperty(() => SelectedPayment, value, RefreshCashboxes); }
        }

        #endregion

        #region DialogSettings

        public override int Height => 600;

        public override int MinHeight => 450;

        public override int MinWidth => 750;

        public override int Width => 800;

        #endregion

        private IOpenFileDialogService OpenFileDialogService => GetService<IOpenFileDialogService>("ImportFromExcelFileDialogService");

        private IMessenger Messenger { get; }

        private IExcelImportEngine<ReceiveViewItem> ExcelImportEngine { get; }

        private IExcelImportEngine<ReceiveViewItem> UkrposhtaExcelImportEngine { get; }

        private IExcelImportEngine<ReceiveViewItem> MeestTtnExcelImportEngine { get; }

        public static void BuildMetadata(MetadataBuilder<ProcessReceivedMoneyViewModel> builder)
        {
            builder.Property(x => x.SelectedCashboxId).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.SelectedPayment).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.ReceivedOn).Required(() => Resources.RequiredErrorMessage);
        }

        protected override async Task HandleLoadedAsync()
        {
            ReceivedOnMinValue = DateTime.Today.AddDays(-30);
            ReceivedOnMaxValue = DateTime.Today;

            Items = new ObservableCollection<ReceiveViewItem>();
            Items.CollectionChanged += ItemsCollectionChanged;

            Messages = new ObservableCollection<ReceiveViewMessageItem>();

            RecognizeTypes = Dictionaries.GetItems<MoneyReceiveRecognizeType>().ToReadOnlyObservableCollection();

            cashboxes = await WebClient.ExecuteApiRequestAsync(new QueryCashboxes(), true);

            SelectedPayment = Dictionaries.GetItemById<Payment>(Payment.CashId);

            RefreshSummaryItems();

            Title = "Проведение ДС";
        }

        protected override void OnInitializeInDesignMode()
        {
            base.OnInitializeInDesignMode();

            Items = new ObservableCollection<ReceiveViewItem>();
            Messages = new ObservableCollection<ReceiveViewMessageItem>();

            Random random = new Random();

            for (int i = 0; i < 5; i++)
            {
                Items.Add(new ReceiveViewItem(i + 1, random.Next(100000, 300000).ToString())
                {
                    PlannedAmount = random.Next(1000000),
                    ActualAmount = random.Next(1000000),
                    Recognized = random.NextDouble() > 0.5,
                    Processed = random.NextDouble() < 0.5,
                    OrderState = OrderStatus.Done
                });
            }

            Items.Add(new ReceiveViewItem(5, random.Next(100000, 300000).ToString())
            {
                PlannedAmount = 1000000,
                ActualAmount = 1000000,
                Recognized = random.NextDouble() > 0.5,
                Processed = random.NextDouble() < 0.5,
                OrderState = OrderStatus.Done
            });

            Messages = new ObservableCollection<ReceiveViewMessageItem>
            {
                new ReceiveViewMessageItem(1, "Error 1"),
                new ReceiveViewMessageItem(2, "Error 2"),
                new ReceiveViewMessageItem(3, "Error 3"),
                new ReceiveViewMessageItem(4, "Error 4"),
                new ReceiveViewMessageItem(5, "Error 5")
            };

            RefreshSummaryItems();
        }

        protected override async Task HandleOkAsync()
        {
            ReceiveViewItem[] toProcess = Items.Where(x => !x.Processed).ToArray();

            if (toProcess.Length == 0)
            {
                MessageFacadeService.ShowNotificationWarning("Все записи уже обработаны");
                return;
            }

            decimal amountToProcess = toProcess.Sum(x => x.ActualAmount);
            string cashboxName = Cashboxes.First(x => x.Id == SelectedCashboxId).DisplayValue;

            if (!MessageFacadeService.Confirm($"Провести {CurrencyFormatingRules.ToUahStr(amountToProcess, "C2")} на кассу \"{cashboxName}\"?"))
            {
                return;
            }

            if (ReceivedOn != DateTime.Today
                && !ShowValidationResultView("Предупреждения", new[] { new ValidationResultItem($"Дата документа ПКО будет {ReceivedOn:dd.MM.yy} 09:00, а не сегодня", false) }))
            {
                return;
            }

            using CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

            ProgressScreenViewModel progressViewModel = new ProgressScreenViewModel("Обработка", toProcess.Length);

            Task<int> task = ProcessItemsAsync(toProcess, progressViewModel, cancellationTokenSource.Token);

            DialogDocumentManagerService.ShowView("ProgressScreenView", progressViewModel);

            if (!progressViewModel.IsOk)
            {
                cancellationTokenSource.Cancel();
            }

            try
            {
                int processedCount = await task;

                MessageFacadeService.ShowNotificationInfo($"Обработано {processedCount} {DeclOfNum(processedCount)}");
            }
            catch (OperationCanceledException)
            {
                MessageFacadeService.ShowNotificationWarning($"Отмена. Обработано {progressViewModel.ProcessedCount} {DeclOfNum(progressViewModel.ProcessedCount)}");
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to process receive money items");
                MessageFacadeService.ShowNotificationError("Ошибка при обработке данных");
            }

            RefreshSummaryItems();
        }

        private static IEnumerable<ValidationResultItem> ValidateUkrposhtaOrders(
            IReadOnlyCollection<string> barcodes,
            IReadOnlyCollection<OrderDto> orders,
            IReadOnlyDictionary<string, decimal> actualAmounts)
        {
            HashSet<string> notFoundBarcodes = barcodes.ToHashSet();

            notFoundBarcodes.ExceptWith(orders.Select(x => x.PackageTtn));

            foreach (string barcode in notFoundBarcodes)
            {
                yield return new ValidationResultItem($"ШК {barcode} не найден в заказах", true);
            }

            foreach (OrderDto order in orders)
            {
                if (order.Pko != 0)
                {
                    yield return new ValidationResultItem($"Заказ №{order.Id} уже оплачен", true);
                }
                else if (actualAmounts.TryGetValue(order.PackageTtn, out decimal amount))
                {
                    if (order.GetPrices().ToPay.Uah < amount)
                    {
                        yield return new ValidationResultItem($"Заказ №{order.Id}: слишком большой размер платежа", true);
                    }
                }
            }
        }

        private static IEnumerable<ValidationResultItem> ValidateOrders(IReadOnlyCollection<int> orderIds, IReadOnlyCollection<OrderDto> orders, IReadOnlyCollection<ReceiveViewItem> items)
        {
            HashSet<int> notFoundOrderIds = orderIds.ToHashSet();

            notFoundOrderIds.ExceptWith(orders.Select(x => x.Id));

            foreach (int orderId in notFoundOrderIds)
            {
                yield return new ValidationResultItem($"Заказ №{orderId} не найден", true);
            }

            foreach (OrderDto order in orders)
            {
                ReceiveViewItem item = items.FirstOrDefault(x => x.OrderId == order.Id);

                if (order.Pko != 0)
                {
                    yield return new ValidationResultItem($"Заказ №{order.Id} уже оплачен", true);
                }
                else if (item != null)
                {
                    if (order.GetPrices().ToPay.Uah < item.ActualAmount)
                    {
                        yield return new ValidationResultItem($"Заказ №{order.Id}: слишком большой размер платежа", true);
                    }

                    if (order.AfterpaymentAmount > 0
                        && (order.CarryId == CarryType.NpDeliveryId || order.CarryId == CarryType.NpWarehouseId || order.CarryId == CarryType.NpPostBoxId)
                        && item.CodCommission == null)
                    {
                        yield return new ValidationResultItem($"По заказу №{order.Id} не заполнена колонка \"Комиссия\".", true);
                    }
                }
            }
        }

        private async Task<int> ProcessItemsAsync(
            IReadOnlyCollection<ReceiveViewItem> toProcess,
            ProgressScreenViewModel progressViewModel,
            CancellationToken cancellationToken)
        {
            int processedItems = 0;

            foreach (ReceiveViewItem item in toProcess)
            {
                await ProcessItemAsync(item);

                processedItems++;

                progressViewModel.SetProcessedCount(processedItems);

                cancellationToken.ThrowIfCancellationRequested();
            }

            return processedItems;
        }

        private async Task ProcessItemAsync(ReceiveViewItem item)
        {
            LockResponse<OrderDto> lockResponse = await LockOrderAsync(item.Number, item.OrderId);

            if (lockResponse != null)
            {
                if (lockResponse.Success)
                {
                    bool processed = await AddOrderPaymentAsync(item.Number, item.OrderId, item.ActualAmount, item.CodCommission, item.Received ?? ReceivedOn!.Value);

                    await UnlockOrderAsync(item.Number, item.OrderId);

                    item.Processed = processed;

                    if (processed)
                    {
                        RefreshSummaryItems();
                    }
                }
                else
                {
                    AddMessage(item.Number, $"Заказ заблокирован пользователем: {lockResponse.Dto.EmployeeLock.Name}");
                }
            }
        }

        private async Task<bool> AddOrderPaymentAsync(int number, int orderId, decimal amount, decimal? codComission, DateTime received)
        {
            bool success = false;

            try
            {
                CreateOrderPayment gatewayRequest = new CreateOrderPayment(
                    orderId,
                    Currency.Uah.Id,
                    SelectedPayment.Id,
                    SelectedCashboxId.Value,
                    amount,
                    received,
                    null,
                    codComission: codComission,
                    prepayment: true);

                Result<OrderPaymentResultDto> result = await WebClient.ExecuteApiRequestAsync(gatewayRequest);

                Messenger.Send(new OrderMessage(result.Data.Order, MessageType.Changed));
                Messenger.Send(new OrderPaymentMessage(result.Data.OrderPayment, MessageType.Added));

                success = true;
            }
            catch (UnexpectedSatusException exception)
            {
                AddMessage(number, string.Join(". ", exception.GetErrorItems().Select(x => x.Message)));
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed while making payment {message}", Resources.ServerUnavailable);
                AddMessage(number, $"Ошибка при внесении оплаты. {Resources.ServerUnavailable}");
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed while making payment");
                AddMessage(number, "Ошибка при внесении оплаты");
            }

            return success;
        }

        private async Task<LockResponse<OrderDto>> LockOrderAsync(int number, int orderId)
        {
            LockResponse<OrderDto> response = null;

            try
            {
                response = await WebClient.ExecuteApiRequestAsync(new LockOrder(orderId, false));
                Messenger.Send(new OrderMessage(response.Dto, MessageType.Changed));
            }
            catch (UnexpectedSatusException exception) when (exception.Args.HttpStatusCode == HttpStatusCode.Forbidden)
            {
                AddMessage(number, "Ошибка при блокировании заказа. У вас нет прав на выполнение операции");
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to block order");
                AddMessage(number, "Ошибка при блокировании заказа");
            }

            return response;
        }

        private async Task UnlockOrderAsync(int number, int orderId)
        {
            try
            {
                LockResponse<OrderDto> unlockResponse = await WebClient.ExecuteApiRequestAsync(new UnlockOrder(orderId));
                Messenger.Send(new OrderMessage(unlockResponse.Dto, MessageType.Changed));
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to unblock order");
                AddMessage(number, "Ошибка при разблокировании заказа");
            }
        }

        private async Task ShowRecognizeDialogAsync(MoneyReceiveRecognizeType recognizeType)
        {
            if (!Items.Any() || MessageFacadeService.Confirm("Список оплат будет очищен, продолжить?"))
            {
                Items.Clear();

                IReadOnlyCollection<ReceiveViewItem> items = Array.Empty<ReceiveViewItem>();

                if (recognizeType == MoneyReceiveRecognizeType.Excel)
                {
                    items = await ImportFromExcelAsync();
                }
                else if (recognizeType == MoneyReceiveRecognizeType.ExcelUkrposhta)
                {
                    items = await UkrposhtaImportFromExcelAsync();
                }
                else if(recognizeType == MoneyReceiveRecognizeType.ExcelMeestTtn)
                {
                    items = await MeestTtnExcelAsync();
                }
                else
                {
                    ReceiveMoneyViewModel receiveViewModel = SizeableDialogDocumentManagerService.ShowView<ReceiveMoneyViewModel>(recognizeType, this);

                    if (receiveViewModel.IsOk && receiveViewModel.Items?.Any() == true)
                    {
                        items = receiveViewModel.Items;
                    }
                }

                if (items.Any())
                {
                    foreach (ReceiveViewItem receiveViewItem in items)
                    {
                        Items.Add(receiveViewItem.Clone());
                    }

                    RefreshSummaryItems();
                }
            }
        }

        private async Task<IReadOnlyCollection<ReceiveViewItem>> UkrposhtaImportFromExcelAsync()
        {
            IReadOnlyCollection<ReceiveViewItem> items = Array.Empty<ReceiveViewItem>();

            if (!OpenFileDialogService.ShowDialog())
            {
                return items;
            }

            if (!OpenFileDialogService.File.Exists)
            {
                MessageFacadeService.ShowNotificationWarning("Файл не существует");
                return items;
            }

            string filePath = OpenFileDialogService.GetFullFileName();

            List<ValidationResultItem> errors = new List<ValidationResultItem>();

            try
            {
                ExcelImportResult<ReceiveViewItem> importResult = UkrposhtaExcelImportEngine.ImportFromXlsx(filePath);

                errors.AddRange(importResult.Errors);

                if (importResult.IsSuccess)
                {
                    foreach (IReadOnlyCollection<ReceiveViewItem> receiveViewItems in importResult.ResultItems.Section(100))
                    {
                        errors.AddRange(await ProcessItemsAsync(receiveViewItems));
                    }
                }

                if (errors.Any())
                {
                    ShowValidationResultView("Ошибки при импорте данных", errors);
                }
                else
                {
                    items = importResult.ResultItems;

                    MessageFacadeService.ShowNotificationInfo("Платежи успешно импортированы");
                }
            }
            catch (IOException exception) when (exception.Message.Contains("being used by another process"))
            {
                MessageFacadeService.ShowNotificationError($"Файл {filePath} занят другим процессом");
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to import {FileName}", filePath);
                MessageFacadeService.ShowNotificationError("Ошибка при импорте");
            }

            return items;
        }

        private async Task<IReadOnlyCollection<ReceiveViewItem>> ImportFromExcelAsync()
        {
            IReadOnlyCollection<ReceiveViewItem> items = Array.Empty<ReceiveViewItem>();

            if (!OpenFileDialogService.ShowDialog())
            {
                return items;
            }

            if (!OpenFileDialogService.File.Exists)
            {
                MessageFacadeService.ShowNotificationWarning("Файл не существует");
                return items;
            }

            string filePath = OpenFileDialogService.GetFullFileName();

            List<ValidationResultItem> errors = new List<ValidationResultItem>();

            try
            {
                ExcelImportResult<ReceiveViewItem> importResult = ExcelImportEngine.ImportFromXlsx(filePath);

                errors.AddRange(importResult.Errors);

                if (importResult.IsSuccess)
                {
                    foreach (IReadOnlyCollection<ReceiveViewItem> receiveViewItems in importResult.ResultItems.Section(100))
                    {
                        errors.AddRange(await ProcessItemsAsync(receiveViewItems));
                    }
                }

                if (errors.Any())
                {
                    ShowValidationResultView("Ошибки при импорте данных", errors);
                }
                else
                {
                    items = importResult.ResultItems;

                    MessageFacadeService.ShowNotificationInfo("Платежи успешно импортированы");
                }
            }
            catch (IOException exception) when (exception.Message.Contains("being used by another process"))
            {
                MessageFacadeService.ShowNotificationError($"Файл {filePath} занят другим процессом");
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to import {FileName}", filePath);
                MessageFacadeService.ShowNotificationError("Ошибка при импорте");
            }

            return items;
        }

        private async Task<IReadOnlyCollection<ReceiveViewItem>> MeestTtnExcelAsync()
        {
            IReadOnlyCollection<ReceiveViewItem> items = Array.Empty<ReceiveViewItem>();

            if (!OpenFileDialogService.ShowDialog())
            {
                return items;
            }

            if (!OpenFileDialogService.File.Exists)
            {
                MessageFacadeService.ShowNotificationWarning("Файл не существует");
                return items;
            }

            string filePath = OpenFileDialogService.GetFullFileName();

            List<ValidationResultItem> errors = new List<ValidationResultItem>();

            try
            {
                ExcelImportResult<ReceiveViewItem> importResult = MeestTtnExcelImportEngine.ImportFromXlsx(filePath);

                errors.AddRange(importResult.Errors);

                if (importResult.IsSuccess)
                {
                    foreach (IReadOnlyCollection<ReceiveViewItem> receiveViewItems in importResult.ResultItems.Section(100))
                    {
                        errors.AddRange(await ProcessItemsAsync(receiveViewItems));
                    }
                }

                if (errors.Any())
                {
                    ShowValidationResultView("Ошибки при импорте данных", errors);
                }
                else
                {
                    items = importResult.ResultItems;

                    MessageFacadeService.ShowNotificationInfo("Платежи успешно импортированы");
                }
            }
            catch (IOException exception) when (exception.Message.Contains("being used by another process"))
            {
                MessageFacadeService.ShowNotificationError($"Файл {filePath} занят другим процессом");
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to import {FileName}", filePath);
                MessageFacadeService.ShowNotificationError("Ошибка при импорте");
            }


            return items;
        }

        private async Task<IReadOnlyCollection<ValidationResultItem>> ProcessItemsAsync(IReadOnlyCollection<ReceiveViewItem> items)
        {
            HashSet<int> orderIds = items.Select(x => x.OrderId).ToHashSet();
            HashSet<string> trackNumbers = items.Where(x => !string.IsNullOrWhiteSpace(x.TrackNumber)).Select(x => x.TrackNumber).ToHashSet();

            OrderFilteringItem orderFilteringItem = new OrderFilteringItem(string.Empty, new List<int>())
            {
                OrderNumbers = orderIds.All(x => x > 0) ? string.Join(", ", orderIds) : null,
                TrackNumbers = string.Join(",", trackNumbers)
            };

            List<OrderDto> orders = await WebClient.ExecuteApiRequestAsync(new QueryOrders(orderFilteringItem)).GetPagedResultDataAsync();

            IReadOnlyCollection<ValidationResultItem> errors;

            if (trackNumbers.Any())
            {
                Dictionary<string, decimal> ttnAmouns = items
                    .ToDictionary(x => x.TrackNumber, y => y.ActualAmount);

                errors = ValidateUkrposhtaOrders(trackNumbers, orders, ttnAmouns).ToList();
            }
            else
            {
                errors = ValidateOrders(orderIds, orders, items).ToList();
            }

            if (errors.Any())
            {
                return errors;
            }

            foreach (OrderDto order in orders)
            {
                List<ReceiveViewItem> orderItems;

                if (trackNumbers.Any())
                {
                    orderItems = items.Where(x => x.TrackNumber == order.PackageTtn).ToList();
                }
                else
                {
                    orderItems = items.Where(x => x.OrderId == order.Id).ToList();
                }

                foreach (ReceiveViewItem orderItem in orderItems)
                {
                    orderItem.OrderState = Dictionaries.GetItemById<OrderStatus>(order.StateId);
                    orderItem.PlannedAmount = order.GetPrices().ToPay.Uah;
                    orderItem.Recognized = true;

                    if (orderItem.OrderId == 0)
                    {
                        orderItem.OrderId = order.Id;
                    }
                }
            }

            return errors;
        }

        private void RefreshCashboxes()
        {
            Cashboxes = cashboxes
                .ForIncome(WebClient, Currency.Uah.Id, SelectedPayment?.Id)
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToReadOnlyObservableCollection();
        }

        private void AddMessage(int number, string message)
        {
            ReceiveViewMessageItem messageItem = new ReceiveViewMessageItem(number, message);
            Messages.Add(messageItem);
            CurrentMessage = messageItem;
        }

        private void RefreshSummaryItems()
        {
            SummaryItems = GetSummaryItems();

            IEnumerable<SummaryViewItem> GetSummaryItems()
            {
                int processed = 0;
                decimal processedAmount = 0;
                decimal actualAmount = 0;

                foreach (ReceiveViewItem item in Items.ToArray())
                {
                    actualAmount += item.ActualAmount;

                    if (item.Processed)
                    {
                        processed++;
                        processedAmount += item.ActualAmount;
                    }
                }

                yield return new SummaryViewItem("Всего", $"{Items.Count:D} шт.{Environment.NewLine}{CurrencyFormatingRules.ToUahStr(actualAmount, "C2")}");
                yield return new SummaryViewItem("Проведено", $"{processed:D} шт.{Environment.NewLine}{CurrencyFormatingRules.ToUahStr(processedAmount, "C2")}");
            }
        }

        private void RemoveAll()
        {
            if (Items.Any() && MessageFacadeService.Confirm("Вы уверены?"))
            {
                Items.Clear();
                Messages.Clear();
            }
        }

        private void RemoveItem(ReceiveViewItem item)
        {
            if (MessageFacadeService.Confirm("Вы уверены?"))
            {
                Items.Remove(item);
            }
        }

        private void ItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            RefreshSummaryItems();
        }

        private string DeclOfNum(int x)
        {
            return WordEndingHelper.GetWordByNumber(x, new[] { "строка", "строки", "строк" });
        }
    }
}