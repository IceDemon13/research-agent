using System.Threading.Tasks;
using DevExpress.Mvvm;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.Settings.Printing;
using Telemart.Client.Data.Requests.Features.Validation;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.FiscalRegistrar;
using Telemart.Client.FiscalRegistrar.Abstraction;
using Telemart.Client.FiscalRegistrar.Abstraction.TransferObjects;
using Telemart.Client.ViewModels.Common;
using Telemart.Common.ErrorHandling;
using Telemart.Common.Localization;

namespace Telemart.Client.Helpers
{
    public sealed class RroPrintHelper : IRroPrintHelper
    {
        private readonly IErrorHandler _errorHandler;
        private readonly IFiscalRegistrarClientFactory _fiscalRegistrarClientFactory;
        private readonly IWebClient _webClient;
        private readonly ILogger<RroPrintHelper> _logger;
        private readonly IPrintingSettingsStore _printingSettingsStore;

        public RroPrintHelper(
            IWebClient webClient,
            IFiscalRegistrarClientFactory fiscalRegistrarClientFactory,
            IErrorHandler errorHandler,
            IPrintingSettingsStore printingSettingsStore,
            ILogger<RroPrintHelper> logger)
        {
            _webClient = webClient;
            _fiscalRegistrarClientFactory = fiscalRegistrarClientFactory;
            _printingSettingsStore = printingSettingsStore;
            _errorHandler = errorHandler;
            _logger = logger;
        }

        public async Task<Result> SentCheckAsync(string fiscalId, string phone, string email, int cashboxId, ISupportServices parent, bool? showView)
        {
            IDocumentManagerService dialogDocumentManagerService = parent.ServiceContainer.GetService<IDocumentManagerService>("DialogDocumentManagerService", ServiceSearchMode.PreferParents);

            IFiscalRegistrarClient fiscalClient = await _fiscalRegistrarClientFactory.CreateAsync(FiscalRegistrarType.Software.Type, null, default);

            if (fiscalClient != null)
            {
                Result result = await _errorHandler.HandleErrorsAsync(
                    _ => _webClient.ExecuteApiRequestAsync(new PhonesValidation(new[] { phone })),
                    null,
                    null,
                    parent,
                    false,
                    showDialog: false,
                    showError: false);

                PrintRroCheckSettingsDto settings = await fiscalClient.GetPrintRroChecksSettingsAsync();

                PrintingSettingsInfo printingSettings = await _printingSettingsStore.LoadAsync();

                bool isOk = false;

                bool sentEmail = settings.Email && !string.IsNullOrEmpty(email),
                    sentPhone = settings.Phone && result?.IsSuccess == true,
                    sentPrinter = settings.Printer || result?.IsSuccess != true || string.IsNullOrEmpty(email);
                string emailText = email;
                string phoneNumber = phone;

                if (showView == true || printingSettings.ShowChoosePrintRroCheck != false)
                {
                    ChoosePrintRroCheckParameter parameter = new ChoosePrintRroCheckParameter(email, phone, sentEmail, sentPhone, sentPrinter);

                    ChoosePrintRroCheckViewModel viewModel = dialogDocumentManagerService.ShowView<ChoosePrintRroCheckViewModel>(parameter, parent);

                    isOk = viewModel.IsOk;

                    sentPhone = viewModel.Phone;
                    sentEmail = viewModel.Email;
                    sentPrinter = viewModel.Printer;
                    emailText = viewModel.EmailText;
                    phoneNumber = viewModel.PhoneNumber;
                }
                else
                {
                    isOk = true;
                }

                if (isOk)
                {
                    int cashboxCheckboxId = cashboxId;

                    if (sentEmail)
                    {
                        sentEmail = await _errorHandler.HandleErrorsAsync(
                            ct => fiscalClient.SendToEmailsAsync(
                                fiscalId,
                                new[] { emailText },
                                cashboxCheckboxId,
                                ct),
                            "отправке чека на почту",
                            "Чек на почту отправлен",
                            parent,
                            true,
                            onSuccess: (_, _) =>
                            {
                                _logger.LogInformation($"Чек {fiscalId} на почту {emailText} отправлен");
                                return Task.CompletedTask;
                            });
                    }

                    if (sentPhone)
                    {
                        PhoneNumber phoneNum = new PhoneNumber(phoneNumber);

                        sentPhone = await _errorHandler.HandleErrorsAsync(
                            ct => fiscalClient.SendByPhoneAsync(fiscalId, phoneNum.InternationalNumberWithoutPlus, cashboxCheckboxId, ct),
                            "отправке чека по sms",
                            "Чек по sms отправлен",
                            parent,
                            true,
                            onSuccess: (_, _) =>
                            {
                                _logger.LogInformation($"Чек {fiscalId} по sms на номер {phoneNum.InternationalNumberWithoutPlus} отправлен");
                                return Task.CompletedTask;
                            });
                    }

                    if (sentPrinter)
                    {
                        sentPrinter = await _errorHandler.HandleErrorsAsync(
                            async ct =>
                            {
                                await fiscalClient.PrintChequeAsync(cashboxCheckboxId, fiscalId, ct);
                                return true;
                            },
                            "печати чека РРО",
                            "Чек РРО распечатан",
                            parent,
                            true,
                            onSuccess: (_, _) =>
                            {
                                _logger.LogInformation($"Чек {fiscalId} отправлен на мечать");
                                return Task.CompletedTask;
                            });
                    }

                    if (sentEmail || sentPhone || sentPrinter)
                    {
                        return Result.Success();
                    }
                }
            }

            return Result.Error("Ошибка при отправки чека", string.Empty);
        }
    }
}