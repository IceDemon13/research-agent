using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Telemart.Client.Business;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.Cashbox;
using Telemart.Client.Data.Requests.Features.Cashbox.Actions;
using Telemart.Client.Data.Requests.Features.FiscalDocument;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.FiscalRegistrar;
using Telemart.Client.FiscalRegistrar.Abstraction;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects.Cashbox;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Common;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;
using Telemart.Fiscal.Client.Extensions;
using Telemart.Fiscal.Client.TransferObjects;

namespace Telemart.Client.ViewModels.Store.FiscalRegistrar.FiscalRegistrarCashbox
{
    public class FiscalRegistrarCashboxViewModel : TelemartDialogViewModelBase
    {
        public FiscalRegistrarCashboxViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IFiscalRegistrarClientFactory fiscalRegistrarClientFactory,
            IErrorHandler errorHandler)
            : base(webClient, dictionaries, messageFacadeService)
        {
            FiscalRegistrarClientFactory = fiscalRegistrarClientFactory;
            ErrorHandler = errorHandler;
        }

        public decimal InCashbox
        {
            get { return GetProperty(() => InCashbox); }
            private set { SetProperty(() => InCashbox, value); }
        }

        public bool IsClosed
        {
            get { return GetProperty(() => IsClosed); }
            private set { SetProperty(() => IsClosed, value); }
        }

        public ReadOnlyObservableCollection<FiscalRegistrarOperation> Operations
        {
            get { return GetProperty(() => Operations); }
            private set { SetProperty(() => Operations, value); }
        }

        public FiscalRegistrarOperation SelectedOperation
        {
            get { return GetProperty(() => SelectedOperation); }
            set { SetProperty(() => SelectedOperation, value); }
        }

        private IFiscalRegistrarClientFactory FiscalRegistrarClientFactory { get; }

        private IErrorHandler ErrorHandler { get; }

        public static void BuildMetadata(MetadataBuilder<FiscalRegistrarCashboxViewModel> builder)
        {
            builder.Property(x => x.SelectedOperation).Required(() => Resources.RequiredErrorMessage);
        }

        protected override async Task HandleLoadedAsync()
        {
            Operations = Dictionaries.GetItems<FiscalRegistrarOperation>().ToReadOnlyObservableCollection();

            Result<IFiscalRegistrarClient> clientResult = await ErrorHandler.HandleErrorsAsync(
                ct => FiscalRegistrarClientFactory.CreateAsync(ct),
                "Определении настроек РРО",
                null,
                this,
                true);

            if (!clientResult.IsSuccess)
            {
                Close();
                return;
            }

            int cashboxId = clientResult.Data.Settings.CashboxId;

            int retryCount = 0;

            while (retryCount < 2)
            {
                CashboxDto cashbox = await WebClient.ExecuteApiRequestAsync(new QueryCashbox(cashboxId));

                IsClosed = cashbox.Session?.Closed ?? true;

                retryCount++;
                bool success = false;
                Exception ex = null;

                var inCashBox = await ErrorHandler.HandleErrorsAsync(
                    ct => clientResult.Data.GetCashStockAsync(cashboxId, ct),
                    "запросе остатка по кассе из РРО",
                    "Запрос остатка суммы в кассе выполнен",
                    this,
                    true,
                    showDialog: false,
                    showNotification: false,
                    onSuccess: (r, ct) =>
                    {
                        InCashbox = r;
                        success = true;
                        return Task.CompletedTask;
                    },
                    onError: (e, _) =>
                    {
                        ex = e;
                        return Task.CompletedTask;
                    },
                    showError: false);

                if (success)
                {
                    break;
                }

                if (ex is UnexpectedErrorException)
                {
                    MessageFacadeService.ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) }, this);
                    Close();
                    break;
                }
                else if (ex is UnexpectedSatusException unexpectedStatusException)
                {
                    IReadOnlyCollection<ValidationResultItem> errorItems =
                        unexpectedStatusException.GetErrorItems();

                    if (errorItems.Any(x => x.Message.Contains("Зміну не відкрито")))
                    {
                        if (!MessageFacadeService.Confirm("На стороне Checkbox смена закрыта.\nЗакрыть смену в Telemart.Client?"))
                        {
                            Close();
                            break;
                        }

                        await ErrorHandler.HandleErrorsAsync(
                            (_) => WebClient.ExecuteApiRequestAsync(new EnsureCloseCashboxSession(cashboxId)),
                            "закрытии смены",
                            "Смена закрыта",
                            this,
                            false,
                            onSuccess: (_, _) =>
                            {
                                IsClosed = true;
                                return Task.CompletedTask;
                            });

                        await Task.Delay(1000);
                    }
                    else if (errorItems.Any(x => !x.Message.Contains("Выполнять операцию при закрытой смене запрещено")))
                    {
                        MessageFacadeService.ShowValidationResultView("Ошибки при запросе остатка по кассе", errorItems, this);
                        Close();
                        break;
                    }
                }

                InCashbox = cashbox.Session?.Amount ?? 0;
            }

            await base.HandleLoadedAsync();

            Title = "Касса РРО";
        }

        protected override Task HandleOkAsync()
        {
            switch (SelectedOperation?.Id)
            {
                case FiscalRegistrarOperation.CashCollectionId:
                    if (InCashbox <= 0)
                    {
                        MessageFacadeService.ShowNotificationWarning("Недостаточно средств для инкассации");
                        return Task.CompletedTask;
                    }

                    FiscalRegistrarCashboxCashCollectionViewModel viewModel = DialogDocumentManagerService.ShowView<FiscalRegistrarCashboxCashCollectionViewModel>(null, this);

                    if (viewModel.IsOk)
                    {
                        IsOk = true;
                        Close();
                    }

                    return Task.CompletedTask;
                case FiscalRegistrarOperation.CloseSessionId:
                    return CloseSessionAsync();

                case FiscalRegistrarOperation.OpenSessionId:
                    return OpenSessionAsync();

                case FiscalRegistrarOperation.XReportId:
                    return XReportAsync();

                case FiscalRegistrarOperation.ZReportId:
                    return ZReportAsync();

                default:
                    return Task.CompletedTask;
            }
        }

        private async Task CloseSessionAsync()
        {
            Result<IFiscalRegistrarClient> clientResult = await ErrorHandler.HandleErrorsAsync(
                ct => FiscalRegistrarClientFactory.CreateAsync(ct),
                "Определении настроек РРО",
                null,
                this,
                true);

            if (!clientResult.IsSuccess)
            {
                return;
            }

            int cashboxId = clientResult.Data.Settings.CashboxId;

            bool getStateResult = await ErrorHandler.HandleErrorsAsync(
                ct => clientResult.Data.GetStateAsync(cashboxId, ct),
                "проверке статуса РРО",
                "Проверка статуса РРО прошла",
                this,
                true);

            if (!getStateResult)
            {
                return;
            }

            DelayedConfirmViewModel viewModel = DialogDocumentManagerService.ShowView<DelayedConfirmViewModel>($"Будет сформирован Z-отчет и в сейф будет перемещено {CurrencyFormatingRules.ToUahStr(InCashbox)}", this);

            if (!viewModel.IsOk)
            {
                return;
            }

            Result<CashboxDto> closeSessionResult = await ErrorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new CloseCashboxSession(cashboxId, InCashbox)),
                "закрытии смены в 1C",
                "Смена в 1C закрыта",
                this,
                true);

            if (closeSessionResult == null)
            {
                return;
            }

            await ErrorHandler.HandleErrorsAsync(
                async ct =>
                {
                    await clientResult.Data.CloseSessionAsync(cashboxId, ct);

                    return true;
                },
                "закрытии смены в РРО",
                "Смена в РРО закрыта",
                this,
                true,
                onSuccess: async (_, _) =>
                {
                    await WebClient.ExecuteApiRequestAsync(new CreateFiscalDocument(FiscalDocumentExtentions.ZReportToDocument(cashboxId, InCashbox)));

                    IsOk = true;
                    Close();
                },
                onError: async (_, _) =>
                {
                    await WebClient.ExecuteApiRequestAsync(new OpenCashboxSession(cashboxId));
                });
        }

        private async Task OpenSessionAsync()
        {
            Result<IFiscalRegistrarClient> clientResult = await ErrorHandler.HandleErrorsAsync(
                ct => FiscalRegistrarClientFactory.CreateAsync(ct),
                "Определении настроек РРО",
                null,
                this,
                true);

            if (!clientResult.IsSuccess)
            {
                return;
            }

            int cashboxId = clientResult.Data.Settings.CashboxId;

            CashboxDto cashbox = await WebClient.ExecuteApiRequestAsync(new QueryCashbox(cashboxId));

            decimal amount = cashbox.Session?.Amount ?? 0;

            if (!MessageFacadeService.Confirm($"Будет напечатан нулевой чек и из сейфа будет перемещено {CurrencyFormatingRules.ToUahStr(amount)}"))
            {
                return;
            }

            Result<CashboxDto> openSessionResult = await ErrorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new OpenCashboxSession(cashboxId)),
                "открытии смены в 1С",
                "Смена в 1С открыта",
                this,
                true);

            if (openSessionResult == null)
            {
                return;
            }

            if (clientResult.IsSuccess)
            {
                await ErrorHandler.HandleErrorsAsync(
                    async ct =>
                    {
                        await clientResult.Data.OpenSessionAsync(cashboxId, amount, ct);

                        try
                        {
                            await clientResult.Data.XReportAsync(cashboxId, ct);
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError(ex, "Failed get X-Report during open session.");
                        }

                        return true;
                    },
                    "открытии смены в РРО",
                    "Смена в РРО открыта",
                    this,
                    true,
                    onSuccess: async (_, _) =>
                    {
                        FiscalDocumentCreateDto fiscalDocumentCreateDto = new FiscalDocumentCreateDto()
                        {
                            Amount = amount,
                            CashboxId = cashboxId,
                            Real = true,
                            TypeId = FiscalDocumentType.ServiceReceive.Id,
                            Payments = new[]
                            {
                                new FiscalDocumentPaymentDto
                                    { TypeId = FiscalPaymentType.CashPayment.Id, Amount = amount }
                            }
                        };

                        await WebClient.ExecuteApiRequestAsync(new CreateFiscalDocument(fiscalDocumentCreateDto));

                        IsOk = true;
                        Close();
                    },
                    onError: async (ex, _) =>
                    {
                        if (clientResult.Data is not ProgrammicalFiscalRegistrarClient)
                        {
                            if (!(ex is UnexpectedSatusException unexpectedSatusException)
                                || unexpectedSatusException.GetErrorItems().Any(x => x.Message != "Касир вже працює з даною касою"))
                            {
                                await WebClient.ExecuteApiRequestAsync(
                                    new CloseCashboxSession(cashboxId, amount));
                            }
                        }
                    });
            }
        }

        private async Task XReportAsync()
        {
            if (!MessageFacadeService.Confirm("Вы действительно желаете напечатать X отчет"))
            {
                return;
            }

            Result<IFiscalRegistrarClient> clientResult = await ErrorHandler.HandleErrorsAsync(
                ct => FiscalRegistrarClientFactory.CreateAsync(ct),
                "Определении настроек РРО",
                null,
                this,
                true);

            if (clientResult.IsSuccess)
            {
                int cashboxId = clientResult.Data.Settings.CashboxId;

                await ErrorHandler.HandleErrorsAsync(
                    async ct =>
                    {
                        await clientResult.Data.XReportAsync(cashboxId, ct);

                        return true;
                    },
                    "печати X отчета",
                    "X отчет напечатан",
                    this,
                    true,
                    onSuccess: (_, _) =>
                    {
                        IsOk = true;
                        Close();

                        return Task.CompletedTask;
                    });
            }
        }

        private async Task ZReportAsync()
        {
            if (!MessageFacadeService.Confirm("Вы действительно желаете напечатать Z отчет"))
            {
                return;
            }

            Result<IFiscalRegistrarClient> clientResult = await ErrorHandler.HandleErrorsAsync(
                ct => FiscalRegistrarClientFactory.CreateAsync(ct),
                "Определении настроек РРО",
                null,
                this,
                true);

            if (!clientResult.IsSuccess)
            {
                return;
            }

            if (clientResult.Data.Settings.FiscalConnectionTypeId == FiscalConnectionType.HardwareId)
            {
                MessageFacadeService.ShowNotificationWarning("Печать Z-отчета для аппаратного РРО не поддерживается.");
                return;
            }

            int cashboxId = clientResult.Data.Settings.CashboxId;

            CashboxDto cashbox = await WebClient.ExecuteApiRequestAsync(new QueryCashbox(cashboxId));

            bool isClosed = cashbox.Session?.Closed ?? true;

            if (!isClosed)
            {
                MessageFacadeService.ShowNotificationWarning("Печать Z-отчета недоступна при открытой смене.");
                return;
            }

            if (string.IsNullOrEmpty(cashbox.Session?.LastZreportId))
            {
                MessageFacadeService.ShowMessageBoxWarning("Данные о Z-отчете отсутствуют. Возможна смена была закрыта автоматически. Для получения Z-отчета обратитесь в бухгалтерию.");
                return;
            }

            await ErrorHandler.HandleErrorsAsync(
                async ct =>
                {
                    await clientResult.Data.ZReportAsync(cashboxId, cashbox.Session?.LastZreportId, ct);

                    return true;
                },
                "печати Z-отчета",
                "Z-отчет напечатан",
                this,
                true,
                onSuccess: (_, _) =>
                {
                    IsOk = true;
                    Close();

                    return Task.CompletedTask;
                });
        }
    }
}