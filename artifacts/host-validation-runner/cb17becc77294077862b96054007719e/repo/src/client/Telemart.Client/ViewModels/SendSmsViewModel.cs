using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Telemart.Client.Business.SmsTemplates;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Utils;
using Telemart.Client.Data.Requests.Features.Order;
using Telemart.Client.Data.Requests.Features.Payments;
using Telemart.Client.Data.Requests.Features.Sms;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;
using Telemart.Common.TransferObjects;

namespace Telemart.Client.ViewModels
{
    public sealed class SendSmsViewModel : TelemartDialogViewModelBase
    {
        private const int MessageSmsLength = 140;
        private const int MessageViberLength = 500;

        private int _orderId;
        private OrderDto _order;
        private int? _priceTypeId;
        private int? _serviceRequestId;
        private IReadOnlyCollection<CreditOfferDto> _allCreditOffers;

        public SendSmsViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IErrorHandler errorHandler,
            ILogger<SendSmsViewModel> logger)
            : base(webClient, dictionaries, messageFacadeService, logger)
        {
            ErrorHandler = errorHandler;
            Subdivisions.AddRange(Dictionaries.GetItems<Subdivision>());
            SmsText = string.Empty;
            ViberText = string.Empty;
        }

        public SendSmsViewModel()
        {
        }

        #region Dependency Properties

        public string Phone
        {
            get { return GetProperty(() => Phone); }
            set { SetProperty(() => Phone, value); }
        }

        public string Phone2
        {
            get { return GetProperty(() => Phone2); }
            set { SetProperty(() => Phone2, value); }
        }

        public decimal? Amount
        {
            get { return GetProperty(() => Amount); }
            set { SetProperty(() => Amount, value); }
        }

        public CreditOfferDto CreditOffer
        {
            get { return GetProperty(() => CreditOffer); }
            set { SetProperty(() => CreditOffer, value); }
        }

        public ObservableCollection<string> Phones
        {
            get { return GetProperty(() => Phones); }
            private set { SetProperty(() => Phones, value); }
        }

        public Subdivision Subdivision
        {
            get { return GetProperty(() => Subdivision); }
            set { SetProperty(() => Subdivision, value); }
        }

        public ObservableRangeCollection<Subdivision> Subdivisions { get; } = new ObservableRangeCollection<Subdivision>();

        public ISmsTemplate Template
        {
            get { return GetProperty(() => Template); }
            set { SetProperty(() => Template, value, TemplateChanged); }
        }

        public ObservableCollection<ISmsTemplate> Templates
        {
            get { return GetProperty(() => Templates); }
            private set { SetProperty(() => Templates, value); }
        }

        public IReadOnlyCollection<SendMessageType> SendMessageTypes
        {
            get { return GetProperty(() => SendMessageTypes); }
            private set { SetProperty(() => SendMessageTypes, value); }
        }

        public string SmsText
        {
            get { return GetProperty(() => SmsText); }
            set { SetProperty(() => SmsText, value); }
        }

        public string ViberText
        {
            get { return GetProperty(() => ViberText); }
            set { SetProperty(() => ViberText, value); }
        }

        public int? SendMessageTypeId
        {
            get { return GetProperty(() => SendMessageTypeId); }
            set { SetProperty(() => SendMessageTypeId, value, SendMessageTypeChanged); }
        }

        public int? TemplateId
        {
            get { return GetProperty(() => TemplateId); }
            set { SetProperty(() => TemplateId, value, () => RaisePropertyChanged(nameof(SendMessageTypeId))); }
        }

        public ReadOnlyObservableCollection<CreditOfferDto> CreditOffers
        {
            get { return GetProperty(() => CreditOffers); }
            private set { SetProperty(() => CreditOffers, value); }
        }

        public bool AmountVisible => Template?.TemplateId is SmsTemplate.LiqPayOrderPrepaymentId
            or SmsTemplate.MonoPayOrderPrepaymentId
            or SmsTemplate.PortmoneOrderPrepaymentId
            or SmsTemplate.NovaPayOrderPrepaymentId
            or SmsTemplate.NovaPayOrderPrepaymentNovakLegalEntityId
            or SmsTemplate.NovaPayOrderPrepaymentTkachLegalEntityId
            or SmsTemplate.PrivatPartialOrderPrepaymentId
            or SmsTemplate.PrivatCreditOrderPrepaymentId;

        public bool AmountReadOnly => Template?.TemplateId is SmsTemplate.PrivatPartialOrderPrepaymentId or SmsTemplate.PrivatCreditOrderPrepaymentId;

        public bool CreditOffersVisible => Template?.TemplateId is SmsTemplate.PrivatPartialOrderPrepaymentId or SmsTemplate.PrivatCreditOrderPrepaymentId;

        public bool SmsTextEnabled => !AmountVisible && SendMessageTypeId is SendMessageType.HybridId or SendMessageType.SmsId;

        public bool ViberTextEnabled => !AmountVisible && SendMessageTypeId is SendMessageType.HybridId or SendMessageType.ViberId;

        #endregion

        private IErrorHandler ErrorHandler { get; }

        public static void BuildMetadata(MetadataBuilder<SendSmsViewModel> builder)
        {
            builder.Property(x => x.Subdivision).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.Phone).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.SmsText)
                .MatchesInstanceRule(
                    (x, y) => (y.SendMessageTypeId != SendMessageType.SmsId
                                                && y.SendMessageTypeId != SendMessageType.HybridId)
                                               || !string.IsNullOrEmpty(x),
                    () => Resources.RequiredErrorMessage)
                .MaxLength(MessageSmsLength, () => $"Длина сообщения не должна превышать {MessageSmsLength} символов");
            builder.Property(x => x.ViberText)
                .MatchesInstanceRule(
                    (x, y) => (y.SendMessageTypeId != SendMessageType.ViberId
                               && y.SendMessageTypeId != SendMessageType.HybridId)
                              || !string.IsNullOrEmpty(x),
                    () => Resources.RequiredErrorMessage)
                .MaxLength(MessageViberLength, () => $"Длина сообщения не должна превышать {MessageViberLength} символов");
            builder.Property(x => x.Amount)
                .MatchesInstanceRule((x, y) => y.AmountVisible == false || x > 0, () => "Значение должно быть больше 0");
            builder.Property(x => x.SendMessageTypeId)
                .Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.CreditOffer)
                .MatchesInstanceRule(
                    (x, y) => x is not null || (y.Template?.TemplateId is not SmsTemplate.PrivatPartialOrderPrepaymentId and not SmsTemplate.PrivatCreditOrderPrepaymentId),
                    () => "Кредитное предложение должно быть заполнено");
        }

        protected override Task HandleLoadedAsync()
        {
            SendMessageTypes = Dictionaries.GetItems<SendMessageType>();

            if (Templates == null || Templates.Count == 0)
            {
                MessageFacadeService.ShowNotificationWarning("Нет ни одного шаблона для данного документа");
            }

            Title = "Отправить SMS";
            return Task.CompletedTask;
        }

        protected override async Task HandleOkAsync()
        {
            if ((SendMessageTypeId != SendMessageType.ViberId && !string.IsNullOrWhiteSpace(Template?.SmsText) && SmsText != Template?.SmsText)
                || (SendMessageTypeId != SendMessageType.SmsId && !string.IsNullOrWhiteSpace(Template?.ViberText) && ViberText != Template?.ViberText))
            {
                if (MessageFacadeService.Confirm("Текст сообщения отличается от шаблона. Все равно отправить?"))
                {
                    TemplateId = null;
                }
                else
                {
                    return;
                }
            }

            Func<CancellationToken, Task<Result<object>>> actionFunc;

            if (TemplateId.HasValue)
            {
                actionFunc = _ => WebClient.ExecuteApiRequestAsync(new SendSmsTemplate(TemplateId.Value, Phone, _orderId, _serviceRequestId, Amount, CreditOffer?.Month, Template.Data, SendMessageTypeId!.Value));
            }
            else
            {
                string smsText = !string.IsNullOrWhiteSpace(SmsText) && SendMessageTypeId is SendMessageType.SmsId or SendMessageType.HybridId
                    ? SmsText
                    : null;

                string viberText = !string.IsNullOrWhiteSpace(ViberText) && SendMessageTypeId is SendMessageType.ViberId or SendMessageType.HybridId
                    ? ViberText
                    : null;

                actionFunc = _ => WebClient.ExecuteApiRequestAsync(new SendSms(Phone, smsText, viberText, _orderId, _serviceRequestId, SendMessageTypeId!.Value));
            }

            await ErrorHandler.HandleErrorsAsync(
                actionFunc,
                "отправке сообщения",
                "Сообщение создано (добавлено в очередь на отправку согласно рабочему графику)",
                this,
                true,
                onSuccess: (_, _) =>
                {
                    CloseOk();

                    return Task.CompletedTask;
                });
        }

        protected override void OnInitializeInDesignMode()
        {
            base.OnInitializeInDesignMode();

            Subdivisions.Add(Subdivision.Telemart);
            Subdivision = Subdivisions.First();
            Phone = "0953454323";
            SmsText = "Не смогли к Вам дозвониться по заказу ХХХХХХ. Вы можете связаться с нами по тел. 0443928494 или заказать обратный звонок на сайте.";
            ViberText = "Не смогли к Вам дозвониться по заказу ХХХХХХ. Вы можете связаться с нами по тел. 0443928494 или заказать обратный звонок на сайте.";
        }

        protected override void OnParameterChanged(object p)
        {
            if (IsInDesignMode)
            {
                return;
            }

            SendSmsParameter parameter = (SendSmsParameter)p;

            _orderId = parameter.OrderId;
            _serviceRequestId = parameter.ServiceRequestId;
            _priceTypeId = parameter.PriceTypeId;

            Amount = parameter.Amount;
            Phone = parameter.Phone;
            Phone2 = parameter.Phone2;
            Subdivision = parameter.Subdivision;

            Phones = new ObservableCollection<string>(GetPhones(parameter));
            Templates = new ObservableCollection<ISmsTemplate>(GetSmsTemplates(parameter));
        }

        private static IEnumerable<string> GetPhones(SendSmsParameter p)
        {
            yield return p.Phone;

            if (!string.IsNullOrEmpty(p.Phone2))
            {
                yield return p.Phone2;
            }
        }

        private static IEnumerable<ISmsTemplate> GetSmsTemplates(SendSmsParameter p)
        {
            if (p.Templates != null)
            {
                foreach (ISmsTemplate template in p.Templates)
                {
                    yield return template;
                }
            }
        }

        private void SendMessageTypeChanged()
        {
            switch (SendMessageTypeId)
            {
                case SendMessageType.HybridId:
                    SmsText = string.IsNullOrWhiteSpace(SmsText)
                        ? Template?.SmsText
                        : SmsText;
                    ViberText = string.IsNullOrWhiteSpace(ViberText)
                        ? Template?.ViberText
                        : ViberText;
                    break;
                case SendMessageType.ViberId:
                    SmsText = null;
                    ViberText = string.IsNullOrWhiteSpace(ViberText)
                        ? Template?.ViberText
                        : ViberText;
                    break;
                case SendMessageType.SmsId:
                    SmsText = string.IsNullOrWhiteSpace(SmsText)
                        ? Template?.SmsText
                        : SmsText;
                    ViberText = null;
                    break;
                default:
                    SmsText = null;
                    ViberText = null;
                    break;
            }

            RaisePropertiesChanged(nameof(SmsText), nameof(ViberText), nameof(SmsTextEnabled), nameof(ViberTextEnabled));
        }

        private async void TemplateChanged()
        {
            try
            {
                if (Template is null)
                {
                    return;
                }

                string[] validationItems = Template.Validate().ToArray();

                if (validationItems.Any())
                {
                    ClearSelectedTemplate();

                    MessageFacadeService.ShowMessageBoxWarning(validationItems.First());
                    return;
                }

                SmsText = Template.SmsText;
                ViberText = Template.ViberText;
                TemplateId = Template.TemplateId;
                SendMessageTypeId = SendMessageType.HybridId;

                if (Template?.TemplateId is SmsTemplate.PrivatPartialOrderPrepaymentId or SmsTemplate.PrivatCreditOrderPrepaymentId)
                {
                    await CalculateCreditOffersAsync();
                }

                RaisePropertiesChanged(
                    nameof(AmountVisible),
                    nameof(SmsTextEnabled),
                    nameof(ViberTextEnabled),
                    nameof(CreditOffersVisible),
                    nameof(CreditOffer),
                    nameof(AmountReadOnly));
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to process template change");
                MessageFacadeService.ShowNotificationError("Непредвиденная ошибка");
            }
        }

        private async Task CalculateCreditOffersAsync()
        {
            _order ??= await WebClient.ExecuteApiRequestAsync(new QueryOrder(_orderId));
            _allCreditOffers ??= await WebClient.ExecuteApiRequestAsync(new QueryCreditOffers());

            int paymentId = GetCreditPaymentIdBySelectedTemplate();

            List<(int productId, string productName, int? partialPay)> productPartialPays = _order.Products
                .Where(x => x.ProductTypeId != ProductType.GuestProductId)
                .Select(x => (x.Product.Id, x.Product.Name, GetPartialPayByPaymentId(paymentId, x.Product.Prices.FirstOrDefault(z => z.PriceTypeId == _priceTypeId))))
                .ToList();

            int? maxCreditPartCount = productPartialPays
                .Select(x => x.partialPay)
                .DefaultIfEmpty(0)
                .Min() ?? 0;

            List<ValidationResultItem> errors = productPartialPays
                .Where(x => x.partialPay is null)
                .Select(x => new ValidationResultItem($"Товар {x.productName} ({x.productId}) не поддерживает выбранное кредитование", true))
                .ToList();

            if (errors.Any())
            {
                ShowValidationResultView("Ошибка", errors);
                ClearSelectedTemplate();
                return;
            }

            if (maxCreditPartCount is null or 0)
            {
                MessageFacadeService.ShowNotificationError("Товары не поддерживают выбранное кредитование");
                ClearSelectedTemplate();
                return;
            }

            CreditOffers = _allCreditOffers
                .Where(x => x.PaymentId == paymentId && x.Active && x.Month <= maxCreditPartCount)
                .ToReadOnlyObservableCollection();
        }

        private void ClearSelectedTemplate()
        {
            Template = null;
            SmsText = null;
            ViberText = null;
            SendMessageTypeId = null;
            Template = null;
            TemplateId = null;
            CreditOffer = null;
        }

        private int GetCreditPaymentIdBySelectedTemplate()
        {
            return TemplateId switch
            {
                SmsTemplate.PrivatPartialOrderPrepaymentId => Payment.PrivatPartialPayId,
                SmsTemplate.PrivatCreditOrderPrepaymentId => Payment.CreditId,
                _ => throw new NotSupportedException("Calculate credit paymentId for selected template is not supported")
            };
        }

        private int? GetPartialPayByPaymentId(int paymentId, ProductPriceSimpleDto productPriceSimpleDto)
        {
            return paymentId switch
            {
                Payment.PrivatPartialPayId => productPriceSimpleDto?.PartialPayPb,
                Payment.CreditId => productPriceSimpleDto?.PartialPayPb,
                Payment.PumbId => productPriceSimpleDto?.PartialPayPumb,
                _ => null
            };
        }
    }
}