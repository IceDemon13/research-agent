using System;
using System.Threading.Tasks;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Sms;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.ViewModels.Base;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels
{
    public sealed class ResendSmsViewModel : TelemartDialogViewModelBase
    {
        private readonly IErrorHandler _errorHandler;
        private int? _smsTemplateId;
        private int _orderId;

        public ResendSmsViewModel(
            IWebClient webClient,
            IErrorHandler errorHandler,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
            _errorHandler = errorHandler;
        }

        public string Phone
        {
            get { return GetProperty(() => Phone); }
            private set { SetProperty(() => Phone, value); }
        }

        public string ActualPhone
        {
            get { return GetProperty(() => ActualPhone); }
            set { SetProperty(() => ActualPhone, value); }
        }

        public string Content
        {
            get { return GetProperty(() => Content); }
            set { SetProperty(() => Content, value); }
        }

        public SendMessageType SendMessageType
        {
            get { return GetProperty(() => SendMessageType); }
            private set { SetProperty(() => SendMessageType, value); }
        }

        protected override Task HandleLoadedAsync()
        {
            ResendSmsParameter parameter = (ResendSmsParameter)Parameter;

            Phone = parameter.Phone;
            ActualPhone = parameter.Phone;
            Content = parameter.Content;
            _smsTemplateId = parameter.SmsTemplateId;
            _orderId = parameter.OrderId;
            SendMessageType = GetSendMessageType(parameter.ClientContactType);

            Title = "Переотправка смс";

            return base.HandleLoadedAsync();
        }

        protected override async Task HandleOkAsync()
        {
            const string ErrorPrefix = "переотправке смс";
            const string SuccessPrefix = "Сообщение создано (добавлено в очередь\nна отправку согласно рабочему графику)";

            if (string.IsNullOrWhiteSpace(ActualPhone))
            {
                MessageFacadeService.ShowNotificationError("Телефон не заполнен");
                return;
            }

            Result result;

            if (_smsTemplateId.HasValue)
            {
                SendSmsTemplate request = new SendSmsTemplate(
                    _smsTemplateId.Value,
                    ActualPhone,
                    _orderId,
                    null,
                    null,
                    null,
                    null,
                    SendMessageType.Id,
                    Content);

                result = await _errorHandler.HandleErrorsAsync(
                    _ => WebClient.ExecuteApiRequestAsync(request),
                    ErrorPrefix,
                    SuccessPrefix,
                    this,
                    true,
                    showNotification: true);
            }
            else
            {
                SendSms request = new SendSms(ActualPhone, Content, Content, _orderId, null, SendMessageType.Id);

                result = await _errorHandler.HandleErrorsAsync(
                    _ => WebClient.ExecuteApiRequestAsync(request),
                    ErrorPrefix,
                    SuccessPrefix,
                    this,
                    true,
                    showNotification: true);
            }

            if (result?.IsSuccess == true)
            {
                CloseOk();
            }
        }

        private static SendMessageType GetSendMessageType(ClientContactType clientContactType)
        {
            return clientContactType.Id switch
            {
                ClientContactType.SmsId => SendMessageType.Sms,
                ClientContactType.ViberId => SendMessageType.Viber,
                _ => throw new NotSupportedException($"Client contact type with name {clientContactType.Name} not supported")
            };
        }
    }
}