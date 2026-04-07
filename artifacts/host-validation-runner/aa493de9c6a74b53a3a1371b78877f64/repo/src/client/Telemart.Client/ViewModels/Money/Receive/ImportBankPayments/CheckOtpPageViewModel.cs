using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.PrivatBank;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Money.Receive.ImportBankPayments
{
    public sealed class CheckOtpPageViewModel :
        WizardPageViewModelBase<ImportBankPaymentsModel>,
        ISupportWizardNextCommand
    {
        private readonly ILogger _logger;

        public CheckOtpPageViewModel(IWebClient webClient, IMessageFacadeService messageFacadeService, ILogger<CheckOtpPageViewModel> logger)
        {
            WebClient = webClient;
            MessageFacadeService = messageFacadeService;
            HandleLoadedCommand = new AsyncCommand(HandleLoadedAsync);
            _logger = logger;
        }

        #region Commands

        public IAsyncCommand HandleLoadedCommand { get; }

        #endregion

        public bool CanGoForward => Model != null;

        public override string Description { get; } = "Введите код подтверждения";

        public override string Header { get; } = "Подверждение авторизации";

        private IWizardService WizardService => GetService<IWizardService>();

        private IWebClient WebClient { get; }

        private IMessageFacadeService MessageFacadeService { get; }

        public void OnGoForward(CancelEventArgs e)
        {
            IsLongOperationInProgress = true;

            try
            {
                CheckOtp gatewayRequest = new CheckOtp(Model.SessionId, Model.Otp);

                Task<Result<PrivatBankCreateSessionResponse>> task = WebClient.ExecuteApiRequestAsync(gatewayRequest);

                task.ContinueWith(
                    t =>
                    {
                        if (t.IsFaulted)
                        {
                            Exception exception = t.Exception?.Flatten().InnerException;

                            string message;

                            if (exception is UnexpectedSatusException unexpectedSatusException)
                            {
                                message = string.Join(", ", unexpectedSatusException.GetErrorItems().Select(x => x.Message));
                            }
                            else if (exception is UnexpectedErrorException)
                            {
                                message = $"{Resources.ServerConnectError}. {Resources.ServerUnavailable}";
                            }
                            else
                            {
                                message = "Ошибка при подтверждении авторизации";
                            }

                            MessageFacadeService.ShowNotificationError(message);
                        }

                        if (t.Status == TaskStatus.RanToCompletion)
                        {
                            object parentViewModel = ((ISupportParentViewModel)this).ParentViewModel;
                            WizardService.NavigateToView<ImportPageViewModel>(Model, parentViewModel);
                        }

                        IsLongOperationInProgress = false;
                    },
                    TaskScheduler.FromCurrentSynchronizationContext());
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, Resources.ErrorDuringDataLoading);

                MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
                IsLongOperationInProgress = false;
            }
        }

        private Task HandleLoadedAsync()
        {
            return Task.CompletedTask;
        }
    }
}