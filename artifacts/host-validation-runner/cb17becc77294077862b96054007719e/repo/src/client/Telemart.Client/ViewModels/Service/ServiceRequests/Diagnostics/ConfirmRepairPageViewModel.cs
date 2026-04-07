using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.ServiceCenter;
using Telemart.Client.Data.Requests.Features.ServiceRequest.Actions;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.ViewModels.Base;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Service.ServiceRequests.Diagnostics
{
    public sealed class ConfirmRepairPageViewModel :
        WizardPageViewModelBase<DiagnoseServiceRequestModel>,
        ISupportWizardNextCommand,
        ISupportWizardBackCommand
    {
        private readonly ILogger _logger;

        public ConfirmRepairPageViewModel(
            IWebClient webClient,
            IMessageFacadeService messageFacadeService,
            ILogger<ConfirmRepairPageViewModel> logger)
        {
            WebClient = webClient;
            MessageFacadeService = messageFacadeService;

            HandleLoadedCommand = new AsyncCommand(HandleLoadedAsync);
            _logger = logger;
        }

        public ConfirmRepairPageViewModel()
        {
        }

        #region Commands

        public IAsyncCommand HandleLoadedCommand { get; }

        #endregion

        #region INPC

        public ReadOnlyObservableCollection<ComboBoxItem> ServiceCenters
        {
            get { return GetProperty(() => ServiceCenters); }
            private set { SetProperty(() => ServiceCenters, value); }
        }

        #endregion

        public bool CanGoBack => !IsLongOperationInProgress;

        public bool CanGoForward => !IsLongOperationInProgress;

        public override string Description { get; } = "Нажмите \"Далее\"";

        public override string Header { get; } = "Ремонт";

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
                ConfirmRepairServiceRequest gatewayRequest = new ConfirmRepairServiceRequest(Model.Id, Model.ServiceCenterId, Model.AssembledComputerSaveDto);

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
                _logger.LogError(exception, "Error while confirming the service request");
                MessageFacadeService.ShowNotificationError("Ошибка при подтверждении заявки");
                IsLongOperationInProgress = false;
            }
        }

        private async Task HandleLoadedAsync()
        {
            try
            {
                QueryServiceCenters gatewayRequest = new QueryServiceCenters();

                PagedResult<ServiceCenterDto> serviceCenters = await WebClient.ExecuteApiRequestAsync(gatewayRequest, true);

                ServiceCenters = serviceCenters.Data
                    .Where(x => x.Active)
                    .OrderBy(x => x.Name)
                    .Select(x => new ComboBoxItem(x.Id, x.Name))
                    .ToReadOnlyObservableCollection();
            }
            catch (Exception e)
            {
                _logger.LogError(e, Resources.ErrorDuringDataLoading);
                MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
                IsLongOperationInProgress = false;
            }
        }
    }
}