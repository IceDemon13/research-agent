using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.Cashbox;
using Telemart.Client.Data.Requests.Features.Contractor;
using Telemart.Client.Data.Requests.Features.ServiceRequest.Actions;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Helpers;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Cashbox;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Common;
using Telemart.Client.ViewModels.Nomenclature;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Service.ServiceRequests
{
    public sealed class CompensateServiceRequestViewModel : TelemartDialogViewModelBase
    {
        private readonly IMapper _mapper;

        private int serviceRequestId;
        private int? bundleId;
        private IReadOnlyCollection<ServiceRequestCompensationPrice> prices;
        private IReadOnlyCollection<CashboxDto> cashboxes;
        private ContractorDto contractor;
        private OrderDto order;

        public CompensateServiceRequestViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            CompensationHelper compensationHelper,
            IMapper mapper)
            : base(webClient, dictionaries, messageFacadeService)
        {
            CompensationHelper = compensationHelper;
            _mapper = mapper;
            SelectProductCommand = new DelegateCommand(SelectProduct);
            HandlePaymentTypeChangedCommand = new DelegateCommand(HandlePaymentTypeChanged);

            Requisites = new RequisitesViewItem();
        }

        public CompensateServiceRequestViewModel(IMapper mapper)
        {
            _mapper = mapper;
        }

        #region Commands

        public IDelegateCommand SelectProductCommand { get; }

        public IDelegateCommand HandlePaymentTypeChangedCommand { get; }

        #endregion

        #region INPC

        public ReadOnlyObservableCollection<ServiceRequestCompensationType> CompenstateTypes
        {
            get { return GetProperty(() => CompenstateTypes); }
            private set { SetProperty(() => CompenstateTypes, value); }
        }

        public ServiceRequestCompensationType CompensateType
        {
            get { return GetProperty(() => CompensateType); }
            set { SetProperty(() => CompensateType, value, CompensateTypeChanged); }
        }

        public ReadOnlyObservableCollection<ServiceRequestCompensationPrice> CompensationPrices
        {
            get { return GetProperty(() => CompensationPrices); }
            private set { SetProperty(() => CompensationPrices, value); }
        }

        public ServiceRequestCompensationPrice CompensationPrice
        {
            get { return GetProperty(() => CompensationPrice); }
            set { SetProperty(() => CompensationPrice, value, CompensationPriceChanged); }
        }

        public ProductItem Product
        {
            get { return GetProperty(() => Product); }
            set { SetProperty(() => Product, value); }
        }

        public decimal? Amount
        {
            get { return GetProperty(() => Amount); }
            set { SetProperty(() => Amount, value); }
        }

        public string Comment
        {
            get { return GetProperty(() => Comment); }
            set { SetProperty(() => Comment, value); }
        }

        public ComboBoxItem? Cashbox
        {
            get { return GetProperty(() => Cashbox); }
            set { SetProperty(() => Cashbox, value); }
        }

        public ObservableCollection<ComboBoxItem> Cashboxes
        {
            get { return GetProperty(() => Cashboxes); }
            private set { SetProperty(() => Cashboxes, value); }
        }

        public bool IsReadOnlyCompensationPrice
        {
            get { return GetProperty(() => IsReadOnlyCompensationPrice); }
            set { SetProperty(() => IsReadOnlyCompensationPrice, value); }
        }

        public bool HideRequirementPayment
        {
            get { return GetProperty(() => HideRequirementPayment); }
            set { SetProperty(() => HideRequirementPayment, value, () => RaisePropertiesChanged(nameof(CashboxVisible), nameof(PaymentVisible))); }
        }

        public bool IsReadOnlyAmount
        {
            get { return GetProperty(() => IsReadOnlyAmount); }
            set { SetProperty(() => IsReadOnlyAmount, value); }
        }

        public Payment ReturnMoneyPaymentType
        {
            get { return GetProperty(() => ReturnMoneyPaymentType); }
            set { SetProperty(() => ReturnMoneyPaymentType, value, ReturnMoneyPaymentTypeChanged); }
        }

        public ObservableCollection<Payment> ReturnMoneyPaymentTypes
        {
            get { return GetProperty(() => ReturnMoneyPaymentTypes); }
            private set { SetProperty(() => ReturnMoneyPaymentTypes, value); }
        }

        public RequisitesViewItem Requisites
        {
            get { return GetProperty(() => Requisites); }
            set { SetProperty(() => Requisites, value); }
        }

        public bool NeedChooseProduct => CompensateType == ServiceRequestCompensationType.CompensateByProduct;

        public bool ChooseProductEnabled => NeedChooseProduct && bundleId.HasValue;

        public bool CashboxVisible => ReturnMoneyPaymentType?.Id == Payment.CashId && !HideRequirementPayment;

        public bool PaymentVisible => CompensateType?.Id == ServiceRequestCompensationType.CompensateReturnMoney.Id && !HideRequirementPayment;

        #endregion

        private CompensationHelper CompensationHelper { get; }

        public static void BuildMetadata(MetadataBuilder<CompensateServiceRequestViewModel> builder)
        {
            builder.Property(x => x.CompensateType)
                .Required(() => Resources.RequiredErrorMessage);

            builder.Property(x => x.CompensationPrice)
                .MatchesInstanceRule((x, y) => y.CompensateType is null || y.IsReadOnlyCompensationPrice || (!y.IsReadOnlyCompensationPrice && x != null), () => Resources.RequiredErrorMessage);

            builder.Property(x => x.Amount)
                .MatchesInstanceRule((x, y) => y.CompensateType is null || y.IsReadOnlyAmount || (x > 0 && x <= 10_000_000 && !y.IsReadOnlyAmount), () => Resources.RequiredErrorMessage);

            builder.Property(x => x.ReturnMoneyPaymentType)
              .MatchesInstanceRule((x, y) => !(y.PaymentVisible && x is null), () => Resources.RequiredErrorMessage);

            builder.Property(x => x.Cashbox)
             .MatchesInstanceRule((x, y) => !(y.CashboxVisible && x is null), () => Resources.RequiredErrorMessage);

            builder.Property(x => x.Product).MatchesInstanceRule(
                (x, y) => y.CompensateType != ServiceRequestCompensationType.CompensateByProduct || x != null,
                () => Resources.RequiredErrorMessage);
        }

        protected override async Task HandleLoadedAsync()
        {
            ServiceRequestViewItem serviceRequest = (ServiceRequestViewItem)Parameter;

            serviceRequestId = serviceRequest.Id;
            bundleId = serviceRequest.BundleId;

            contractor = await WebClient.ExecuteApiRequestAsync(new QueryContractor(serviceRequest.ContractorId));
            (prices, order) = await CompensationHelper.GetProssibleCompensationPricesAsync(serviceRequest.OrderId, serviceRequest.ProductId);

            IsReadOnlyCompensationPrice = false;
            IsReadOnlyAmount = order.PaymentId == Payment.CashlessNoTaxId || order.PaymentId == Payment.CashlessTaxId || IsReadOnlyCompensationPrice;

            RaisePropertiesChanged(nameof(Amount), nameof(CompensationPrice));

            CompenstateTypes = GetCompensationTypes().ToReadOnlyObservableCollection();

            Product = new ProductItem(serviceRequest.ProductId, serviceRequest.ProductName);

            cashboxes = await WebClient.ExecuteApiRequestAsync(new QueryCashboxes(), true);

            ReturnMoneyPaymentTypes = Dictionaries.GetServiceRequestPaymentTypes(order.PaymentId, order.OrderPayments).ToObservableCollection();

            HideRequirementPayment = order.IsPartialRefund();

            Title = "Обмен";
        }

        protected override async Task HandleOkAsync()
        {
            if (IDataErrorInfoHelper.HasErrors(this))
            {
                return;
            }

            try
            {
                int? productId = CompensateType == ServiceRequestCompensationType.CompensateByProduct
                    ? Product.Id
                    : null;

                ServiceRequestCompensateDto serviceRequestCompensateDto = new ServiceRequestCompensateDto(
                    serviceRequestId,
                    Amount.Value,
                    CompensationPrice.CurrencyId,
                    productId,
                    Comment,
                    Cashbox?.Id,
                    _mapper.Map<RefundRequisitesDto>(Requisites),
                    ReturnMoneyPaymentType?.Id);

                CompensateServiceRequest gatewayRequest = new CompensateServiceRequest(serviceRequestId, serviceRequestCompensateDto);

                Result<ServiceRequestDto> result = await WebClient.ExecuteApiRequestAsync(gatewayRequest);

                if (result.Warnings.Any())
                {
                    MessageFacadeService.ShowNotificationInfo($"Заявка №{serviceRequestId} обменяна c предупреждениями");
                    ShowValidationResultView("Предупрежедения", result.Warnings.Select(x => new ValidationResultItem(x, false)).ToList());
                }
                else
                {
                    MessageFacadeService.ShowNotificationInfo($"Заявка №{serviceRequestId} успешно обменяна");
                }

                IsOk = true;
                Close();
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при обмене заявки");
                ShowValidationResultView("Ошибки при обмене заявки", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to compensate service request");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при обмене заявки");
                Logger.LogError(exception, "Failed to compensate service request");
            }
        }

        private void CompensateTypeChanged()
        {
            CompensationPrices = null;
            CompensationPrice = null;

            CompensationPrices = CompensationHelper
                .GetCompensationPrices(
                    contractor,
                    CompensateType == ServiceRequestCompensationType.CompensateOnBalance,
                    prices)
                .ToReadOnlyObservableCollection();

            if (CompensationPrices.Count == 1)
            {
                CompensationPrice = CompensationPrices.First();
            }

            if (IsReadOnlyCompensationPrice)
            {
                CompensationPrice = CompensationPrices.Where(x => x.CurrencyId == Currency.UahId).OrderByDescending(x => x.Value).First();
            }

            RaisePropertiesChanged(nameof(NeedChooseProduct), nameof(Product), nameof(PaymentVisible), nameof(ReturnMoneyPaymentType));

            if (!PaymentVisible)
            {
                ReturnMoneyPaymentType = null;
            }
        }

        private void CompensationPriceChanged()
        {
            Amount = CompensationPrice?.Value;
        }

        private IEnumerable<ServiceRequestCompensationType> GetCompensationTypes()
        {
            if (Payment.IsCachlessPayment(order.PaymentId) && bundleId is null)
            {
                yield return ServiceRequestCompensationType.CompensateReturnMoney;
            }
            else
            {
                if (contractor.Limit > 1)
                {
                    yield return ServiceRequestCompensationType.CompensateOnBalance;
                }

                yield return ServiceRequestCompensationType.CompensateByProduct;

                if (bundleId is null)
                {
                    yield return ServiceRequestCompensationType.CompensateReturnMoney;
                }
            }
        }

        private void SelectProduct()
        {
            NomenclatureViewOptions options = new NomenclatureViewOptions(
                NomenclatureViewPriceContext.Client,
                Constants.TelemartContractorId,
                NomenclatureViewSelectionMode.Single,
                false);

            NomenclatureViewModel nomenclatureViewModel = SizeableDialogDocumentManagerService.ShowView<NomenclatureViewModel>(options, this);

            if (nomenclatureViewModel.IsOk)
            {
                NomenclatureViewItem product = nomenclatureViewModel.GetSelectedItems().First();
                Product = new ProductItem(product.Id, product.Name);
            }
        }

        private void HandlePaymentTypeChanged()
        {
            Cashboxes = cashboxes
                .ForReturn(Currency.Uah.Id, ReturnMoneyPaymentType?.Id)
                .ForLegalEntity(order.LegalEntity)
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToObservableCollection();
        }

        private void ReturnMoneyPaymentTypeChanged()
        {
            Requisites.Enabled = ReturnMoneyPaymentType?.Id > 0
                                 && ReturnMoneyPaymentType.Id != Payment.CashId
                                 && CompensateType?.Id == ServiceRequestCompensationType.CompensateReturnMoney.Id
                                 && !HideRequirementPayment;

            RaisePropertiesChanged(nameof(Cashbox), nameof(Requisites), nameof(CashboxVisible), nameof(Cashbox));

            if (!CashboxVisible)
            {
                Cashbox = null;
            }
        }

        public class ProductItem : BindableBase
        {
            public ProductItem(int id, string name)
            {
                Id = id;
                Name = name;
            }

            public int Id
            {
                get { return GetProperty(() => Id); }
                set { SetProperty(() => Id, value); }
            }

            public string Name
            {
                get { return GetProperty(() => Name); }
                set { SetProperty(() => Name, value); }
            }

            public override string ToString()
            {
                return Name;
            }
        }
    }
}