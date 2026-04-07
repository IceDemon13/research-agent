using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Validation;

namespace Telemart.Client.ViewModels.Dialogs
{
    public class PreloaderViewModel : TelemartDialogViewModelBase
    {
        private readonly ILogger<PreloaderViewModel> _logger;
        private readonly Progress<string> _progress;
        private bool _isCompleted;
        private bool _isError;
        private bool _canClose;

        public PreloaderViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            ILogger<PreloaderViewModel> logger)
            : base(webClient, dictionaries, messageFacadeService)
        {
            _logger = logger;
            _progress = new Progress<string>(x => Message = x);
        }

        public string Message
        {
            get { return GetProperty(() => Message); }
            private set { SetProperty(() => Message, value); }
        }

        public bool IsSpecialError
        {
            get { return GetProperty(() => IsSpecialError); }
            private set { SetProperty(() => IsSpecialError, value); }
        }

        public override void OnClose(CancelEventArgs e)
        {
            if (_isCompleted || _isError)
            {
                Close();
                return;
            }
            else
            {
                if (_canClose && MessageFacadeService.Confirm("Вы уверены что хотите прервать действие?"))
                {
                    Close();
                    return;
                }

                if (!_canClose)
                {
                    MessageFacadeService.ShowNotificationWarning("Вы не можете закрыть окно пока идет операция");
                }
            }

            e.Cancel = true;
        }

        protected override void OnInitializeInDesignMode()
        {
            Message = "Обработка долгой операции...";

            base.OnInitializeInDesignMode();
        }

        protected override async Task HandleLoadedAsync()
        {
            PreloaderParameter parameter = (PreloaderParameter)Parameter;

            _isCompleted = false;
            _canClose = parameter.CanClose;

            Title = "Ожидайте";

            try
            {
                IEnumerable<ValidationResultItem> validationItems = await await Task.Factory.StartNew(() => parameter.Action(_progress));

                ValidationResultItem[] validationResultItems = validationItems?.ToArray() ?? Array.Empty<ValidationResultItem>();

                if (validationResultItems.Any())
                {
                    if (!string.IsNullOrEmpty(parameter.IsSpecialErrorMessage) && validationResultItems.Any(x => x.Message?.Contains(parameter.IsSpecialErrorMessage) == true))
                    {
                        IsSpecialError = true;
                    }
                    else
                    {
                        MessageFacadeService.ShowValidationResultView(parameter.ValidationDialogTitle, validationResultItems, this);
                    }
                }

                _isCompleted = true;
                IsOk = !validationResultItems.Any();
            }
            catch (UnexpectedSatusException exception)
            {
                _isError = true;
                MessageFacadeService.ShowNotificationError("Ошибка при обработке операции");
                ShowValidationResultView("Ошибка при обработке операции", exception.GetErrorItems());
            }
            catch (Exception e)
            {
                _isError = true;
                _logger.LogError(e, "Failed to execute action");
                MessageFacadeService.ShowNotificationError("Ошибка при обработке операции");
            }

            Close();
        }

        protected override Task HandleOkAsync()
        {
            throw new NotImplementedException();
        }
    }
}