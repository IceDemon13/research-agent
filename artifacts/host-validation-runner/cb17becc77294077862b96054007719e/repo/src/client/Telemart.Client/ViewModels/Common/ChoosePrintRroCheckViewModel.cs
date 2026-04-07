using System.Threading.Tasks;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Common;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Validation;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Properties;
using Telemart.Client.ViewModels.Base;
using Telemart.Common.Constants;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Common
{
    public sealed class ChoosePrintRroCheckViewModel : TelemartDialogViewModelBase
    {
        private readonly IErrorHandler _errorHandler;

        public ChoosePrintRroCheckViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IErrorHandler errorHandler)
            : base(webClient, dictionaries, messageFacadeService)
        {
            _errorHandler = errorHandler;
        }

        #region INPC

        public bool Email
        {
            get { return GetProperty(() => Email); }
            set { SetProperty(() => Email, value, () => RaisePropertyChanged(nameof(EmailText))); }
        }

        public bool Phone
        {
            get { return GetProperty(() => Phone); }
            set { SetProperty(() => Phone, value, () => RaisePropertyChanged(nameof(PhoneNumber))); }
        }

        public bool Printer
        {
            get { return GetProperty(() => Printer); }
            set { SetProperty(() => Printer, value); }
        }

        public string EmailText
        {
            get { return GetProperty(() => EmailText); }
            set { SetProperty(() => EmailText, value); }
        }

        public string PhoneNumber
        {
            get { return GetProperty(() => PhoneNumber); }
            set { SetProperty(() => PhoneNumber, value); }
        }

        #endregion

        public static void BuildMetadata(MetadataBuilder<ChoosePrintRroCheckViewModel> builder)
        {
            builder.Property(x => x.PhoneNumber).MatchesInstanceRule(
                (x, y) => !y.Phone || !string.IsNullOrEmpty(x),
                () => Resources.OrderViewModel_Phone1);

            builder.Property(x => x.EmailText).MatchesInstanceRule(
                (x, y) => !y.Email || !string.IsNullOrEmpty(x), () => Resources.RequiredErrorMessage)
                .MatchesRegularExpression(RegexConstants.EmailRegex, () => Resources.OrderViewModel_Email);
        }

        protected override async Task HandleLoadedAsync()
        {
            ChoosePrintRroCheckParameter parameter = (ChoosePrintRroCheckParameter) Parameter;

            EmailText = parameter!.Email;
            PhoneNumber = parameter.Phone;
            Email = parameter.EmailSetting;
            Phone = parameter.PhoneSetting;
            Printer = parameter.PrinterSetting;

            Title = "Выберите способ отправки чека";

            await base.HandleLoadedAsync();
        }

        protected override async Task HandleOkAsync()
        {
            if (!Phone && !Email && !Printer)
            {
                MessageFacadeService.ShowNotificationWarning("Ничего не выбрано");
                return;
            }

            if (Phone)
            {
                Result result = await _errorHandler.HandleErrorsAsync(
                    _ => WebClient.ExecuteApiRequestAsync(new PhonesValidation(new[] { PhoneNumber })),
                    "валидации телефона",
                    null,
                    this,
                    true,
                    showNotification: false);

                if (result.IsSuccess != true)
                {
                    return;
                }
            }

            CloseOk();
        }
    }
}