using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests;
using Telemart.Client.Data.Requests.Features.Contractor;
using Telemart.Client.Data.Requests.Features.ServiceRequest.Actions;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Common;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.Dictionaries;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Service.ServiceRequests.Diagnostics
{
    public sealed class ConfirmProductReturnPageViewModel :
        WizardPageViewModelBase<DiagnoseServiceRequestModel>,
        ISupportWizardNextCommand,
        ISupportWizardBackCommand
    {
        private readonly IMapper _mapper;
        private readonly ILogger _logger;

        public ConfirmProductReturnPageViewModel(
            IWebClient webClient,
            IMessageFacadeService messageFacadeService,
            CompensationHelper compensationHelper,
            IDictionaries dictionaries,
            IMapper mapper,
            ILogger<ConfirmProductReturnPageViewModel> logger)
        {
            WebClient = webClient;
            MessageFacadeService = messageFacadeService;
            CompensationHelper = compensationHelper;
            Dictionaries = dictionaries;

            HandleLoadedCommand = new AsyncCommand(HandleLoadedAsync);
            _mapper = mapper;
            _logger = logger;
        }

        public ConfirmProductReturnPageViewModel()
        {
        }

        #region Commands

        public IAsyncCommand HandleLoadedCommand { get; }

        #endregion

        public bool IsReadOnly
        {
            get { return GetProperty(() => IsReadOnly); }
            set { SetProperty(() => IsReadOnly, value); }
        }

        public bool CanGoBack => !IsLongOperationInProgress;

        public bool CanGoForward => Model != null && Model.Amount > 0 && Model.CompensationPrice != null;

        public override string Description { get; } = "Укажите сумму возврата";

        public override string Header { get; } = "Возврат товара";

        private IMessageFacadeService MessageFacadeService { get; }

        private CompensationHelper CompensationHelper { get; }

        private IDictionaries Dictionaries { get; }

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
                IRestClientGatewayRequest<Result<ServiceRequestDto>> gatewayRequest = BuildRequest();

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

                            if (t.Result.Warnings?.Any() == true)
                            {
                                Model.Warnings = t.Result.Warnings.Select(x => new ValidationResultItem(x, false)).ToReadOnlyObservableCollection();
                            }
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
                ContractorDto contractor = await WebClient.ExecuteApiRequestAsync(new QueryContractor(Model.ContractorId));

                bool isPresaleContractor = contractor?.Name == Constants.PresaleContractor;

                (IReadOnlyCollection<ServiceRequestCompensationPrice> prices, OrderDto order) = await CompensationHelper.GetProssibleCompensationPricesAsync(Model.OrderId, Model.ProductId, isPresaleContractor);

                IsReadOnly = isPresaleContractor
                             || Payment.IsCreditPayment(order.PaymentId)
                             || Payment.IsCachlessPayment(order.PaymentId)
                             || order.OrderPayments?.Any(x => (Payment.IsCreditPayment(x.PaymentId) || x.PaymentId == PaymentIds.BonusesId) && x.Sign > 0) == true;

                if (isPresaleContractor)
                {
                    Model.CompensationPrices = prices.ToReadOnlyObservableCollection();

                    Model.CompensationPrice = Model.CompensationPrices.First();
                }
                else
                {
                    Model.CompensationPrices = CompensationHelper
                        .GetCompensationPrices(
                            contractor,
                            Model.RequirementPaymentId == null,
                            prices)
                        .ToReadOnlyObservableCollection();

                    if (Model.CompensationPrices.Count == 1)
                    {
                        Model.CompensationPrice = Model.CompensationPrices.First();
                    }

                    if (IsReadOnly && contractor?.Name != Constants.PresaleContractor)
                    {
                        Model.CompensationPrice = Model.CompensationPrices.Where(x => x.CurrencyId == Client.Dictionaries.Currency.UahId).OrderByDescending(x => x.Value).First();
                    }
                }

                if (Model.RequirementPaymentId.HasValue && Model.Requirement == ServiceRequestRequirement.ReturnMoney)
                {
                    Payment payment = Dictionaries.GetItemById<Payment>(Model.RequirementPaymentId.Value);

                    Model.Requisites ??= new RequisitesViewItem();

                    Model.Requisites.Enabled = payment.RefundRequisitesControl;
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, Resources.ErrorDuringDataLoading);
                MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
            }
        }

        private IRestClientGatewayRequest<Result<ServiceRequestDto>> BuildRequest()
        {
            IRestClientGatewayRequest<Result<ServiceRequestDto>> request;

            if (Model.Requirement == ServiceRequestRequirement.ReturnMoney)
            {
                request = new ConfirmReturnServiceRequest(
                    Model.Id,
                    Model.Amount.Value,
                    Model.CompensationPrice.CurrencyId,
                    Model.Comment,
                    _mapper.Map<RefundRequisitesDto>(Model.Requisites));
            }
            else
            {
                request = new ConfirmChangeServiceRequest(Model.Id, Model.Amount.Value, Model.CompensationPrice.CurrencyId, Model.Comment);
            }

            return request;
        }
    }
}