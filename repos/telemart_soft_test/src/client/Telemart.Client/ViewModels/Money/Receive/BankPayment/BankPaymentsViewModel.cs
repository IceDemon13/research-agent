using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Utils;
using Telemart.Client.Common.Utils.Import;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Core.Helpers;
using Telemart.Client.Core.Update;
using Telemart.Client.Data.Requests.Features.BankPayment;
using Telemart.Client.Data.Requests.Features.BankPayment.Actions;
using Telemart.Client.Data.Requests.Features.Cashbox;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.Requests.Features.Order;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.BankPayment;
using Telemart.Client.TransferObjects.Cashbox;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Common;
using Telemart.Client.ViewModels.Money.Receive.ImportBankPayments;
using Telemart.Client.ViewModels.Parser.Dictionary;
using Telemart.Common.ErrorHandling;
using SystemWebClient = System.Net.WebClient;

namespace Telemart.Client.ViewModels.Money.Receive.BankPayment
{
    internal sealed class BankPaymentsViewModel : TelemartViewModelBase, ISupportHotkeys
    {
        public BankPaymentsViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            IErrorHandler errorHandler,
            IExcelImportEngine<BulkCreateBankPaymentViewItem> excelImportEngine,
            IMessenger messenger,
            IOptions<UpdateManagerOptions> updateManagerOptions)
            : base(webClient, dictionaries, messageFacadeService)
        {
            ExcelImportEngine = excelImportEngine;
            ErrorHandler = errorHandler;
            Messenger = messenger;
            Mapper = mapper;
            UpdateManagerOptions = updateManagerOptions;

            CancelFilteringCommand = new DelegateCommand(CancelFiltering);
            RefreshCommand = new AsyncCommand(RefreshAsync);
            ProcessCommand = new AsyncCommand(ProcessAsync);
            IgnoreCommand = new AsyncCommand(IgnoreAsync);
            ImportCommand = new DelegateCommand(Import);
            RecognizeCommand = new DelegateCommand(FillParsedOrders);
            ExportExcelCommand = new DelegateCommand(ExportExcel);
            ImportExcelCommand = new AsyncCommand(ImportExcelAsync);

            Filter = new BankPaymentsFilterViewModel(webClient, dictionaries);

            Messenger.Register<BankPaymentMessage>(this, OnBankPaymentMessage);

            Payments = new ObservableRangeCollection<BankPaymentViewItem>();
        }

        public BankPaymentsViewModel()
        {
        }

        #region Commands

        public IDelegateCommand CancelFilteringCommand { get; }

        public IDelegateCommand ExportExcelCommand { get; }

        public IAsyncCommand ImportExcelCommand { get; }

        public IAsyncCommand RefreshCommand { get; }

        public IAsyncCommand ProcessCommand { get; }

        public IAsyncCommand IgnoreCommand { get; }

        public IDelegateCommand ImportCommand { get; }

        public IDelegateCommand RecognizeCommand { get; }

        #endregion

        #region INPC

        public BankPaymentsFilterViewModel Filter { get; }

        public bool IsSearchPanelClosed
        {
            get { return GetProperty(() => IsSearchPanelClosed); }
            set { SetProperty(() => IsSearchPanelClosed, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Cashboxes
        {
            get { return GetProperty(() => Cashboxes); }
            private set { SetProperty(() => Cashboxes, value); }
        }

        public ReadOnlyObservableCollection<Currency> Currencies
        {
            get { return GetProperty(() => Currencies); }
            private set { SetProperty(() => Currencies, value); }
        }

        public ReadOnlyObservableCollection<BankPaymentState> States
        {
            get { return GetProperty(() => States); }
            private set { SetProperty(() => States, value); }
        }

        public ObservableRangeCollection<BankPaymentViewItem> Payments
        {
            get { return GetProperty(() => Payments); }
            set { SetProperty(() => Payments, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Employees
        {
            get { return GetProperty(() => Employees); }
            set { SetProperty(() => Employees, value); }
        }

        public ObservableCollection<BankPaymentViewItem> SelectedPayments { get; } = new ObservableCollection<BankPaymentViewItem>();

        public BankPaymentViewItem CurrentPayment
        {
            get { return GetProperty(() => CurrentPayment); }
            set { SetProperty(() => CurrentPayment, value); }
        }

        public bool IsColumnChooserVisible
        {
            get { return GetProperty(() => IsColumnChooserVisible); }
            set { SetProperty(() => IsColumnChooserVisible, value); }
        }

        #endregion
        private IMapper Mapper { get; }

        private IMessenger Messenger { get; }

        private IErrorHandler ErrorHandler { get; }

        private IDocumentManagerService SizeableDialogDocumentManagerService => GetService<IDocumentManagerService>("SizeableDialogDocumentManagerService", ServiceSearchMode.PreferParents);

        private IDocumentManagerService DialogDocumentManagerService => GetService<IDocumentManagerService>("DialogDocumentManagerService", ServiceSearchMode.PreferLocal);

        private IDialogService WizardDialogService => GetService<IDialogService>("WizardDialogService", ServiceSearchMode.PreferParents);

        private IOpenFileDialogService OpenFileDialogService => GetService<IOpenFileDialogService>("ImportFromExcelFileDialogService");

        private ISaveFileDialogService SaveFileDialogService => GetService<ISaveFileDialogService>("ExcelSaveFileDialogService", ServiceSearchMode.PreferParents);

        private IExcelImportEngine<BulkCreateBankPaymentViewItem> ExcelImportEngine { get; }

        private IOptions<UpdateManagerOptions> UpdateManagerOptions { get; }

        public bool HandleHotkey(HotkeyMessage msg)
        {
            bool handled = false;

            if (msg.ModifierKeys == ModifierKeys.Alt)
            {
                switch (msg.Key)
                {
                    case Key.L:
                        IsSearchPanelClosed = !IsSearchPanelClosed;
                        handled = true;
                        break;
                }
            }
            else if (msg.Key == Key.F9 && msg.ModifierKeys == ModifierKeys.None)
            {
                ProcessCommand.Execute(null);
            }
            else
            {
                switch (msg.HotkeyMessageType)
                {
                    case HotkeyMessageType.Refresh:
                        RefreshCommand.Execute(null);
                        break;

                    case HotkeyMessageType.ShowColumnChooser:
                        IsColumnChooserVisible = !IsColumnChooserVisible;
                        handled = true;
                        break;
                }
            }

            return handled;
        }

        protected override async Task HandleLoadedAsync()
        {
            IsSearchPanelClosed = false;

            Currencies = Dictionaries.GetCurrencies().ToReadOnlyObservableCollection();
            States = Dictionaries.GetItems<BankPaymentState>().ToReadOnlyObservableCollection();

            await Filter.RefreshAsync();
            CancelFilteringCommand.Execute(null);

            await QueryEmployees();

            async Task QueryEmployees()
            {
                PagedResult<EmployeeDto> employees = await WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true);

                Employees = employees.Data
                    .Select(x => new ComboBoxItem(x.Id, x.Name))
                    .ToReadOnlyObservableCollection();
            }
        }

        private static string DeclOfNum(int x)
        {
            return WordEndingHelper.GetWordByNumber(x, new[] { "строка", "строки", "строк" });
        }

        private static string ProcessedByNum(int num)
        {
            return WordEndingHelper.GetWordByNumber(num, "Обработана", "Обработано", "Обработано");
        }

        private void CancelFiltering()
        {
            Filter.ResetFilterValues();
            RefreshCommand.Execute(null);
        }

        private async Task RefreshAsync()
        {
            try
            {
                List<CashboxDto> cashboxes = await WebClient.ExecuteApiRequestAsync(new QueryCashboxes(), true);

                Cashboxes = cashboxes
                    .Select(x => new ComboBoxItem(x.Id, x.Name))
                    .ToReadOnlyObservableCollection();

                await Filter.RefreshAsync();

                PagedResult<BankPaymentDto> payments = await WebClient.ExecuteApiRequestAsync(new QueryBankPayments(Filter.GetFilteringItem()));

                Payments.Clear();

                Payments.AddRange(payments.Data.OrderByDescending(x => x.PaidOn).Select(x => Mapper.Map<BankPaymentViewItem>(x)));
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, Resources.ErrorDuringDataLoading);
                MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
            }
        }

        private async Task ProcessAsync()
        {
            if (SelectedPayments?.Any() != true)
            {
                MessageFacadeService.ShowNotificationWarning("Не выбрана ни одна запись");
                return;
            }

            BankPaymentViewItem errorPayment = SelectedPayments.FirstOrDefault(x => x.OrderId == null);

            if (errorPayment != null)
            {
                MessageFacadeService.ShowNotificationWarning("Номер заказа не заполнен");
                CurrentPayment = errorPayment;
                return;
            }

            if (SelectedPayments.Any(x => x.StateId != BankPaymentState.NewId))
            {
                MessageFacadeService.ShowNotificationWarning($"Разрешено проводить платежи только в статусе \"{BankPaymentState.New.Name}\"");
                return;
            }

            BankPaymentsConfirmViewModel confirmViewModel = SizeableDialogDocumentManagerService.ShowView<BankPaymentsConfirmViewModel>(SelectedPayments, this);

            if (!confirmViewModel.IsOk)
            {
                return;
            }

            ProgressScreenViewModel progressViewModel = new ProgressScreenViewModel("Обработка", SelectedPayments.Count);

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

            Task<Task> task = Task.Factory.StartNew(
                async () =>
                {
                    int processed = 0;
                    try
                    {
                        foreach (BankPaymentViewItem payment in SelectedPayments)
                        {
                            await ProcessItemAsync(payment);
                            progressViewModel.SetProcessedCount(++processed);
                            cancellationTokenSource.Token.ThrowIfCancellationRequested();
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        MessageFacadeService.ShowNotificationWarning($"Отмена. {ProcessedByNum(progressViewModel.ProcessedCount)} {progressViewModel.ProcessedCount} {DeclOfNum(progressViewModel.ProcessedCount)}");
                    }
                    catch (Exception exception)
                    {
                        progressViewModel.CancelCommand.Execute(null);
                        Logger.LogError(exception, "Failed to process bank payments");
                        MessageFacadeService.ShowNotificationError("Ошибка при обработке данных");
                    }
                },
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.FromCurrentSynchronizationContext());

            DialogDocumentManagerService.ShowView("ProgressScreenView", progressViewModel);

            if (!progressViewModel.IsOk)
            {
                cancellationTokenSource.Cancel();
            }

            await task.Unwrap();

            if (SelectedPayments.Any(x => x.IsProcessed && x.IsError))
            {
                MessageFacadeService.ShowNotificationWarning("Платежи обработаны с ошибками");
            }
            else
            {
                int processedCount = SelectedPayments.Count(x => x.IsProcessed);

                MessageFacadeService.ShowNotificationInfo($"{ProcessedByNum(processedCount)} {processedCount} {DeclOfNum(processedCount)}");
            }

            SelectedPayments.RemoveAll(x => x.IsProcessed && !x.IsError);
        }

        private async Task ProcessItemAsync(BankPaymentViewItem payment)
        {
            string error;

            if (!payment.OrderId.HasValue)
            {
                error = "Номер заказа не заполенен";
            }
            else
            {
                int orderId = payment.OrderId.Value;

                error = await LockOrderAsync(orderId);

                if (string.IsNullOrEmpty(error))
                {
                    List<string> errors = new List<string>
                    {
                        await AddPaymentAsync(payment.Id, payment.OrderId.Value),
                        await UnlockOrderAsync(orderId)
                    };

                    error = string.Join(", ", errors.Where(x => !string.IsNullOrWhiteSpace(x)));
                }
            }

            payment.ErrorMessage = error;
            payment.IsProcessed = true;
        }

        private async Task<string> AddPaymentAsync(int paymentId, int orderId)
        {
            string error = string.Empty;

            try
            {
                Result<BankPaymentResultDto> result = await WebClient.ExecuteApiRequestAsync(new PayBankPayment(paymentId, orderId));

                Messenger.Send(new OrderMessage(result.Data.Order, MessageType.Changed));
                Messenger.Send(new OrderPaymentMessage(result.Data.OrderPayment, MessageType.Added));
                Messenger.Send(new BankPaymentMessage(result.Data.BankPayment, MessageType.Changed));
            }
            catch (UnexpectedSatusException exception)
            {
                error = string.Join(", ", exception.GetErrorItems().Select(x => x.Message));
            }
            catch (UnexpectedErrorException)
            {
                error = $"Ошибка при внесении оплаты. {Resources.ServerUnavailable}";
            }
            catch (Exception)
            {
                error = "Ошибка при внесении оплаты";
            }

            return error;
        }

        private async Task<string> LockOrderAsync(int orderId)
        {
            string error = string.Empty;

            try
            {
                LockResponse<OrderDto> response = await WebClient.ExecuteApiRequestAsync(new LockOrder(orderId, false));

                if (!response.Success)
                {
                    error = $"Заказ заблокирован пользователем: {response.Dto.EmployeeLock.Name}";
                }

                Messenger.Send(new OrderMessage(response.Dto, MessageType.Changed));
            }
            catch (UnexpectedSatusException exception) when (exception.Args.HttpStatusCode == HttpStatusCode.Forbidden)
            {
                error = "Ошибка при блокировании заказа. У вас нет прав на выполнение операции";
            }
            catch (Exception)
            {
                error = "Ошибка при блокировании заказа";
            }

            return error;
        }

        private async Task<string> UnlockOrderAsync(int orderId)
        {
            string error = string.Empty;

            try
            {
                LockResponse<OrderDto> unlockResponse = await WebClient.ExecuteApiRequestAsync(new UnlockOrder(orderId));
                Messenger.Send(new OrderMessage(unlockResponse.Dto, MessageType.Changed));
            }
            catch (Exception)
            {
                error = "Ошибка при разблокировании заказа";
            }

            return error;
        }

        private async Task IgnoreAsync()
        {
            if (SelectedPayments?.Any() != true)
            {
                MessageFacadeService.ShowNotificationWarning("Не выбрана ни одна запись");
                return;
            }

            if (!MessageFacadeService.Confirm("Вы уверены, что не хотите учитывать выбранные платежи?"))
            {
                return;
            }

            foreach (BankPaymentViewItem payment in SelectedPayments)
            {
                if (payment.StateId != BankPaymentState.NewId)
                {
                    payment.ErrorMessage = "Разрешено обрабатывать платежи только в статусе новый";
                }
                else
                {
                    try
                    {
                        Result<BankPaymentDto> result = await WebClient.ExecuteApiRequestAsync(new IgnoreBankPayment(payment.Id));

                        if (result.Warnings.Any())
                        {
                            payment.ErrorMessage = string.Join(", ", result.Warnings);
                        }

                        Messenger.Send(new BankPaymentMessage(result.Data, MessageType.Changed));
                    }
                    catch (UnexpectedSatusException exception)
                    {
                        payment.ErrorMessage = string.Join(", ", exception.GetErrorItems().Select(x => x.Message));
                    }
                    catch (UnexpectedErrorException)
                    {
                        payment.ErrorMessage = Resources.ServerUnavailable;
                    }
                    catch (Exception)
                    {
                        payment.ErrorMessage = "Ошибка при обработке платежа";
                    }
                }

                payment.IsProcessed = true;
            }

            if (SelectedPayments.Any(x => x.IsProcessed && x.IsError))
            {
                MessageFacadeService.ShowNotificationWarning("Платежи обработаны с ошибками");
            }
            else
            {
                int processedCount = SelectedPayments.Count(x => x.IsProcessed);

                MessageFacadeService.ShowNotificationInfo($"{ProcessedByNum(processedCount)} {processedCount} {DeclOfNum(processedCount)}");
            }

            SelectedPayments.RemoveAll(x => x.IsProcessed && !x.IsError);

            SelectedPayments.RemoveAll(x => x.IsProcessed && !x.IsError);
        }

        private void Import()
        {
            ImportBankPaymentsModel model = new ImportBankPaymentsModel();

            WizardDialogViewModel<ImportBankPaymentsModel> wizardDialogViewModel = new WizardDialogViewModel<ImportBankPaymentsModel>(
                typeof(CredentialsPageViewModel),
                model,
                this);

            MessageResult messageResult = WizardDialogService.ShowDialog(MessageButton.OKCancel, "Импорт платежей", wizardDialogViewModel);

            if (messageResult == MessageResult.OK)
            {
                RefreshCommand.Execute(null);
            }
        }

        private void FillParsedOrders()
        {
            foreach (BankPaymentViewItem item in Payments.Where(x => x.OrderId == null))
            {
                item.OrderId = item.ParsedOrderId;
            }
        }

        private void ExportExcel()
        {
            if (!SaveFileDialogService.ShowDialog())
            {
                return;
            }

            string filePath = SaveFileDialogService.File.GetFullName();

            DownloadFileSetBankPayment(filePath);

            MessageFacadeService.ShowNotificationInfo("Файл успешно сохранен");
        }

        private async Task ImportExcelAsync()
        {
            if (!OpenFileDialogService.ShowDialog())
            {
                return;
            }

            if (!OpenFileDialogService.File.Exists)
            {
                MessageFacadeService.ShowNotificationWarning("Файл не существует");
                return;
            }

            string filePath = OpenFileDialogService.GetFullFileName();

            try
            {
                ExcelImportResult<BulkCreateBankPaymentViewItem> importResult = ExcelImportEngine.ImportFromXlsx(filePath);

                if (!importResult.IsSuccess)
                {
                    MessageFacadeService.ShowValidationResultView("Ошибки при импорте файла", importResult.Errors, this);
                    return;
                }

                if (importResult.Errors.Any())
                {
                    MessageFacadeService.ShowValidationResultView("Предупреждения при импорте файла", importResult.Errors, this);
                }

                List<CashboxDto> cashboxes = await WebClient.ExecuteApiRequestAsync(new QueryCashboxes(), true);

                ComboBoxItem[] cashboxItems = cashboxes
                    .Where(x => (x.AllowedPayments.Any(z => z == Payment.BankId) || x.HasAccount)
                                && x.IsActive
                                && WebClient.AuthenticatedEmployee.AllowCashboxes.Contains(x.Id))
                    .OrderBy(x => x.Name)
                    .Select(x => new ComboBoxItem(x.Id, x.Name))
                    .ToArray();

                SelectItemViewModel viewModel = DialogDocumentManagerService.ShowView<SelectItemViewModel>(new SelectItemParameter(cashboxItems, "Выберите кассу", "Касса"), this);

                if (!viewModel.IsOk)
                {
                    return;
                }

                BulkCreateBankPaymentDto saveDto = new BulkCreateBankPaymentDto(
                    viewModel.SelectedItem.Value.Id,
                    importResult.ResultItems.Select(x => new BulkCreateBankPaymentOrderDto(
                        x.OrderId,
                        x.PaidOn,
                        x.Amount,
                        x.StatementSupport,
                        x.ContractorName,
                        x.Reference,
                        x.Comment,
                        x.Fee,
                        x.TotalAmount,
                        x.PaymentId)).ToList());

                Result<object> result = await ErrorHandler.HandleErrorsAsync(x => WebClient.ExecuteApiRequestAsync(new BulkCreateBankPayment(saveDto)), "сохранении импорта", "импорт сохранен", this, true);

                if (result is not null)
                {
                    await RefreshAsync();
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
        }

        private void OnBankPaymentMessage(BankPaymentMessage message)
        {
            switch (message.MessageType)
            {
                case MessageType.Added:
                    Payments.Insert(0, Mapper.Map<BankPaymentViewItem>(message.Entity));
                    break;
                case MessageType.Changed:
                    Payments.DoActionWithItem(x => x.Id == message.Entity.Id, viewItem => Mapper.Map(message.Entity, viewItem));
                    break;
            }
        }

        private void DownloadFileSetBankPayment(string localFilePath)
        {
            string fileUrl = $"{UpdateManagerOptions.Value.BaseAddress}/templates/set_bank_payment.xlsx";

            using (SystemWebClient client = new SystemWebClient())
            {
                client.DownloadFileAsync(new Uri(fileUrl), localFilePath);
            }
        }
    }
}