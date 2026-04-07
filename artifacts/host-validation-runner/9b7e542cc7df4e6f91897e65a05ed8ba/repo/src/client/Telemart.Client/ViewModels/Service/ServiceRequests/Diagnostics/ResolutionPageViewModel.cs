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
using Telemart.Client.Data.Requests;
using Telemart.Client.Data.Requests.Features.ServiceRequest.Actions;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Service.ServiceRequests.Diagnostics
{
    public sealed class ResolutionPageViewModel :
        WizardPageViewModelBase<DiagnoseServiceRequestModel>,
        ISupportWizardNextCommand
    {
        public ResolutionPageViewModel(
            IMessageFacadeService messageFacadeService,
            IDictionaries dictionaries,
            IWebClient webClient,
            ILogger<ResolutionPageViewModel> logger)
        {
            MessageFacadeService = messageFacadeService;
            Dictionaries = dictionaries;
            WebClient = webClient;
            Logger = logger;

            HandleLoadedCommand = new AsyncCommand(HandleLoadedAsync);
        }

        public ResolutionPageViewModel()
        {
        }

        #region Commands

        public IAsyncCommand HandleLoadedCommand { get; }

        #endregion

        #region INPC

        public ReadOnlyObservableCollection<ServiceRequestResolution> Resolutions
        {
            get { return GetProperty(() => Resolutions); }
            private set { SetProperty(() => Resolutions, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> RejectReasons
        {
            get { return GetProperty(() => RejectReasons); }
            private set { SetProperty(() => RejectReasons, value); }
        }

        #endregion

        public bool CanGoForward => Model != null && Model.Resolution != null;

        public override string Description { get; } = "Примите решение";

        public override string Header { get; } = "Требование";

        private IDocumentManagerService DialogDocumentManagerService => GetService<IDocumentManagerService>("DialogDocumentManagerService", ServiceSearchMode.PreferParents);

        private IWizardService WizardService => GetService<IWizardService>();

        private IMessageFacadeService MessageFacadeService { get; }

        private IDictionaries Dictionaries { get; }

        private IWebClient WebClient { get; }

        private ILogger<ResolutionPageViewModel> Logger { get; }

        public void OnGoForward(CancelEventArgs e)
        {
            object parentViewModel = ((ISupportParentViewModel)this).ParentViewModel;

            switch (Model.Resolution.Id)
            {
                case ServiceRequestResolution.ConfirmedId:
                    {
                        if (Model.Requirement == ServiceRequestRequirement.Repair)
                        {
                            if (!string.IsNullOrWhiteSpace(Model.SerialNumber) && Model.NomenclatureSeriesAccounting)
                            {
                                ServiceRequestNomenclatureSeriesParameter parameter =
                                    new ServiceRequestNomenclatureSeriesParameter(Model.ProductId, Model.ProductName, Model.SerialNumber, true, Model.Id);

                                ServiceRequestNomenclatureSeriesViewModel viewModel = DialogDocumentManagerService.ShowView<ServiceRequestNomenclatureSeriesViewModel>(parameter, this);

                                if (!viewModel.IsOk)
                                {
                                    return;
                                }

                                Model.AssembledComputerSaveDto = viewModel.GetSaveDto();
                            }

                            WizardService.NavigateToView<ConfirmRepairPageViewModel>(Model, parentViewModel);
                        }
                        else if (Model.Requirement == ServiceRequestRequirement.TradeIn)
                        {
                            ConfirmTradeIn();
                        }
                        else
                        {
                            WizardService.NavigateToView<ConfirmProductReturnPageViewModel>(Model, parentViewModel);
                        }

                        break;
                    }

                case ServiceRequestResolution.RejectedId:
                    {
                        WizardService.NavigateToView<RejectPageViewModel>(Model, parentViewModel);
                        break;
                    }

                default:
                    {
                        throw new NotSupportedException();
                    }
            }
        }

        private Task HandleLoadedAsync()
        {
            try
            {
                RejectReasons = Dictionaries.GetItems<ServiceRequestRejectReason>().Select(x => new ComboBoxItem(x.Id, x.Name)).ToReadOnlyObservableCollection();
                Resolutions = new[] { ServiceRequestResolution.Confirmed, ServiceRequestResolution.Rejected }.ToReadOnlyObservableCollection();
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to init service request diagnosis resolution page");
                MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
            }

            return Task.CompletedTask;
        }

        private void ConfirmTradeIn()
        {
            IsLongOperationInProgress = true;

            try
            {
                ConfirmTradeInServiceRequest request = new ConfirmTradeInServiceRequest(Model.Id, Model.BonusAmount);

                Task<Result<ServiceRequestDto>> task = WebClient.ExecuteApiRequestAsync(request);

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
                Logger.LogError(exception, "Error while confirming service request");
                MessageFacadeService.ShowNotificationError("Ошибка при подтверждении заявки");
                IsLongOperationInProgress = false;
            }
        }
    }
}