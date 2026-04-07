using System;
using System.ComponentModel;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.ServiceRequest.Actions;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Service.ServiceRequests.Diagnostics
{
    public sealed class RejectPageViewModel :
        WizardPageViewModelBase<DiagnoseServiceRequestModel>,
        ISupportWizardNextCommand,
        ISupportWizardBackCommand
    {
        private readonly ILogger _logger;

        public RejectPageViewModel(
            IWebClient webClient,
            IMessageFacadeService messageFacadeService,
            ILogger<RejectPageViewModel> logger)
        {
            WebClient = webClient;
            MessageFacadeService = messageFacadeService;

            HandleLoadedCommand = new AsyncCommand(HandleLoadedAsync);

            _logger = logger;
        }

        public RejectPageViewModel()
        {
        }

        #region Commands

        public IAsyncCommand HandleLoadedCommand { get; }

        #endregion

        #region INPC

        #endregion

        public bool CanGoBack => !IsLongOperationInProgress;

        public bool CanGoForward => Model != null && !string.IsNullOrWhiteSpace(Model.RejectReason) && !string.IsNullOrWhiteSpace(Model.RejectAlternative);

        public override string Description { get; } = "Укажите причину отказа";

        public override string Header { get; } = "Отказ";

        private IMessageFacadeService MessageFacadeService { get; }

        private IWebClient WebClient { get; }

        private IWizardService WizardService => GetService<IWizardService>();

        public void OnGoBack(CancelEventArgs e)
        {
        }

        public void OnGoForward(CancelEventArgs e)
        {
            IsLongOperationInProgress = true;

            try
            {
                RejectServiceRequest gatewayRequest = new RejectServiceRequest(Model.Id, Model.RejectReason, Model.RejectAlternative, Model.RejectReasonId.Value);

                Task<Result<ServiceRequestDto>> task = WebClient.ExecuteApiRequestAsync(gatewayRequest);

                task.ContinueWith(
                    t =>
                    {
                        if (t.IsFaulted)
                        {
                            Exception exception = t.Exception?.Flatten().InnerException;
                            Model.ValidationItems = GetValidationItemsFromException(exception).ToReadOnlyObservableCollection();
                        }

                        if (t.Status == TaskStatus.RanToCompletion)
                        {
                            Model.Result = t.Result.Data;
                        }

                        WizardService.NavigateToView<DiagnoseFinishPageViewModel>(Model, ((ISupportParentViewModel)this).ParentViewModel);

                        IsLongOperationInProgress = false;
                    },
                    TaskScheduler.FromCurrentSynchronizationContext());
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error when rejecting service request");
                MessageFacadeService.ShowNotificationError("Ошибка при отклонении заявки");
                IsLongOperationInProgress = false;
            }
        }

        private Task HandleLoadedAsync()
        {
            return Task.CompletedTask;
        }
    }
}