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
using Telemart.Client.Business.Order;
using Telemart.Client.Business.ServiceRequest;
using Telemart.Client.Common;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.Cashbox;
using Telemart.Client.Data.Requests.Features.Order;
using Telemart.Client.Data.Requests.Features.ServiceRequest;
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
    public sealed class ChangeRequirementViewModel : TelemartDialogViewModelBase
    {
        private readonly IMapper _mapper;
        private int _serviceRequestId;
        private int? _serviceRepairTypeId;
        private List<CashboxDto> _cashboxes;
        private OrderDto _orderDto;
        private RequisitesViewItem _serviceRequestRequisites;
        private string _fio;

        public ChangeRequirementViewModel(
            IWebClient webClient,
            IMapper mapper,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
            _mapper = mapper;
            SelectProductCommand = new DelegateCommand(SelectProduct);
            RemoveProductCommand = new DelegateCommand(RemoveProduct);
        }

        public ChangeRequirementViewModel()
        {
        }

        #region Commands

        public IDelegateCommand RemoveProductCommand { get; }

        public IDelegateCommand SelectProductCommand { get; }

        #endregion

        #region INPC

        public ObservableCollection<ComboBoxItem> Cashboxes
        {
            get { return GetProperty(() => Cashboxes); }
            private set { SetProperty(() => Cashboxes, value); }
        }

        public ServiceRequestRequirement RequirementType
        {
            get { return GetProperty(() => RequirementType); }
            set { SetProperty(() => RequirementType, value, RequirementTypeChanged); }
        }

        public int? RepairDays
        {
            get { return GetProperty(() => RepairDays); }
            set { SetProperty(() => RepairDays, value); }
        }

        #region ReturnMoney

        public ComboBoxItem? Cashbox
        {
            get { return GetProperty(() => Cashbox); }
            set { SetProperty(() => Cashbox, value); }
        }

        public RequisitesViewItem Requisites
        {
            get { return GetProperty(() => Requisites); }
            set { SetProperty(() => Requisites, value); }
        }

        public Payment ReturnMoneyPaymentType
        {
            get { return GetProperty(() => ReturnMoneyPaymentType); }
            set { SetProperty(() => ReturnMoneyPaymentType, value, ReturnMoneyPaymentTypeChanged); }
        }

        public bool HideRequirementPayment
        {
            get { return GetProperty(() => HideRequirementPayment); }
            set { SetProperty(() => HideRequirementPayment, value, () => RaisePropertyChanged(nameof(ReturnMoneyVisible))); }
        }

        public bool CashboxVisible => ReturnMoneyPaymentType?.Id == Payment.CashId;

        public bool ReturnMoneyVisible => RequirementType?.Id == ServiceRequestRequirement.ReturnMoneyId && !HideRequirementPayment;

        #endregion

        #region Change

        public int? ChangeOnProductId
        {
            get { return GetProperty(() => ChangeOnProductId); }
            set { SetProperty(() => ChangeOnProductId, value); }
        }

        public string ChangeOnProductName
        {
            get { return GetProperty(() => ChangeOnProductName); }
            set { SetProperty(() => ChangeOnProductName, value); }
        }

        #endregion

        public ReadOnlyObservableCollection<ServiceRequestRequirement> RequirementTypes
        {
            get { return GetProperty(() => RequirementTypes); }
            private set { SetProperty(() => RequirementTypes, value); }
        }

        public ReadOnlyObservableCollection<RepairDays> RepairDaysCollection
        {
            get { return GetProperty(() => RepairDaysCollection); }
            private set { SetProperty(() => RepairDaysCollection, value); }
        }

        public ObservableCollection<Payment> ReturnMoneyPaymentTypes
        {
            get { return GetProperty(() => ReturnMoneyPaymentTypes); }
            private set { SetProperty(() => ReturnMoneyPaymentTypes, value); }
        }

        #endregion

        public static void BuildMetadata(MetadataBuilder<ChangeRequirementViewModel> builder)
        {
            builder.Property(x => x.RequirementType).Required(() => Resources.RequiredErrorMessage);

            builder.Property(x => x.ChangeOnProductId)
                .MatchesInstanceRule((x, y) => y.RequirementType != ServiceRequestRequirement.Change || x.HasValue, () => Resources.RequiredErrorMessage);

            builder.Property(x => x.ChangeOnProductName)
                .MatchesInstanceRule((x, y) => y.RequirementType != ServiceRequestRequirement.Change || !string.IsNullOrWhiteSpace(x), () => Resources.RequiredErrorMessage);

            builder.Property(x => x.ReturnMoneyPaymentType)
                .MatchesInstanceRule((x, y) => y.RequirementType != ServiceRequestRequirement.ReturnMoney || x != null || y.HideRequirementPayment, () => Resources.RequiredErrorMessage);

            builder.Property(x => x.Cashbox)
                .MatchesInstanceRule((x, y) => y.ReturnMoneyPaymentType == null || y.ReturnMoneyPaymentType.Id != Payment.CashId || x != null || y.HideRequirementPayment, () => Resources.RequiredErrorMessage);

            builder.Property(x => x.RepairDays)
                .MatchesInstanceRule((x, y) => y.RequirementType != ServiceRequestRequirement.Repair || x.HasValue, () => Resources.RequiredErrorMessage);
        }

        protected override async Task HandleLoadedAsync()
        {
            ServiceRequestViewItem serviceRequest = (ServiceRequestViewItem)Parameter;

            _serviceRequestId = serviceRequest.Id;
            _serviceRepairTypeId = serviceRequest.ServiceRepairTypeId;
            _serviceRequestRequisites = serviceRequest.Requisites;
            _fio = serviceRequest.Fio;

            _cashboxes = await WebClient.ExecuteApiRequestAsync(new QueryCashboxes(), true);

            _orderDto = await WebClient.ExecuteApiRequestAsync(new QueryOrder(serviceRequest.OrderId));

            if (_orderDto.LegalEntity != null)
            {
                _cashboxes = _cashboxes
                    .ForLegalEntity(_orderDto.LegalEntity)
                    .ToList();
            }

            Subdivision subdivision = Dictionaries.GetItemById<Subdivision>(serviceRequest.SubdivisionId);

            RequirementTypes = subdivision.GetServiceRequestRequirements().ToReadOnlyObservableCollection();

            RepairDaysCollection = Dictionaries.GetItems<RepairDays>().ToReadOnlyObservableCollection();

            RepairDays = serviceRequest.RepairDays;

            int paymentId = serviceRequest.OrderPaymentTypeId is Payment.PortmoneId
                or Payment.LiqPayId
                or Payment.MonoPayId
                or Payment.MonobankId
                or Payment.PumbId
                or Payment.ABankId
                or Payment.NovaPayId
                or Payment.CreditId
                or Payment.PrivatPartialPayId || Payment.IsCachlessPayment(serviceRequest.OrderPaymentTypeId)
                ? serviceRequest.OrderPaymentTypeId
                : Payment.CashId;

            ReturnMoneyPaymentTypes = Dictionaries.GetServiceRequestPaymentTypes(paymentId, _orderDto.OrderPayments).ToObservableCollection();

            HideRequirementPayment = _orderDto.IsPartialRefund();

            Title = "Изменение требования";
        }

        protected override async Task HandleOkAsync()
        {
            try
            {
                ServiceRequestChangeRequirementDto dto = MapToDto(this, new ServiceRequestChangeRequirementDto());

                ServiceRequestDto serviceRequest = await WebClient.ExecuteApiRequestAsync(new QueryServiceRequest(dto.Id));

                if (RequirementType == ServiceRequestRequirement.ReturnMoney && _orderDto.PaymentId == Payment.MonobankId && (DateTime.Now - _orderDto.CompletedOn) > TimeSpan.FromDays(14))
                {
                    if (!MessageFacadeService.Confirm("С момента выполнения заказа прошло более 14 дней. Мы теряем комиссию! Продолжить?"))
                    {
                        return;
                    }

                    serviceRequest.Comment = "Деньги возвращены после 14 дней. Мы потеряли комиссию";
                }

                IsProductRemovedDto productRemovedDto = new IsProductRemovedDto()
                {
                    OrderId = serviceRequest.OrderId,
                    Requirement = dto.Requirement,
                    OldRequirement = serviceRequest.Requirement,
                    ProductId = serviceRequest.ProductId
                };

                Result<object> productRemovedResult = await WebClient.ExecuteApiRequestAsync(new IsProductRemovedRequest(productRemovedDto));

                string warning = productRemovedResult.Warnings.ToList().FirstOrDefault();

                if (warning != null)
                {
                    if (!MessageFacadeService.Confirm(warning, "Сменить требование?"))
                    {
                        return;
                    }
                }

                ChangeServiceRequestRequirement gatewayRequest = new ChangeServiceRequestRequirement(dto);

                await WebClient.ExecuteApiRequestAsync(gatewayRequest);

                MessageFacadeService.ShowNotificationInfo($"Требование заявки №{_serviceRequestId} успешно изменено");

                IsOk = true;
                Close();
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при изменении требования");
                ShowValidationResultView("Ошибки при изменении требования", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to change service request requirement");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to change service request requirement");
                MessageFacadeService.ShowNotificationError("Ошибка при изменении требования");
            }
        }

        private ServiceRequestChangeRequirementDto MapToDto(
            ChangeRequirementViewModel source,
            ServiceRequestChangeRequirementDto target)
        {
            ServiceRequestRequirementBuilder requirementBuilder = new ServiceRequestRequirementBuilder(
                source.RequirementType,
                source.RepairDays,
                _serviceRepairTypeId,
                source.ChangeOnProductId,
                source.ChangeOnProductName,
                source.ReturnMoneyPaymentType,
                source.Cashbox?.DisplayValue ?? string.Empty);

            target.Id = source._serviceRequestId;
            target.Requirement = source.RequirementType.Id;
            target.RepairDays = source.RepairDays;
            target.RequirementText = requirementBuilder.GetRequirementText();

            switch (source.RequirementType.Id)
            {
                case ServiceRequestRequirement.ChangeId:
                    target.ProductNewId = source.ChangeOnProductId;
                    break;
                case ServiceRequestRequirement.ReturnMoneyId:

                    if (ReturnMoneyVisible)
                    {
                        int? paymentId = source.ReturnMoneyPaymentType.Id > 0
                            ? source.ReturnMoneyPaymentType.Id
                            : null;

                        target.RequirementPaymentId = paymentId;
                        target.RequirementCashboxId = paymentId == Payment.CashId
                            ? source.Cashbox!.Value.Id
                            : null;

                        target.Requisites = _mapper.Map<RefundRequisitesDto>(source.Requisites);
                    }

                    break;
            }

            return target;
        }

        private void ReturnMoneyPaymentTypeChanged(Payment obj)
        {
            Cashboxes = _cashboxes
                .ForReturn(Currency.Uah.Id, ReturnMoneyPaymentType?.Id)
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToObservableCollection();

            CalculateRequisitesEnabled();

            RaisePropertiesChanged(nameof(Cashbox), nameof(CashboxVisible));
        }

        private void RemoveProduct()
        {
            ChangeOnProductId = null;
            ChangeOnProductName = null;
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

                ChangeOnProductId = product.Id;
                ChangeOnProductName = product.Name;
            }
        }

        private void RequirementTypeChanged()
        {
            CalculateRequisitesEnabled();

            RaisePropertiesChanged(nameof(RepairDays), nameof(ChangeOnProductId), nameof(ChangeOnProductName), nameof(ReturnMoneyPaymentType), nameof(ReturnMoneyVisible), nameof(Cashbox));

            if (!ReturnMoneyVisible)
            {
                Cashbox = null;
                ReturnMoneyPaymentType = null;
            }
        }

        private void CalculateRequisitesEnabled()
        {
            if (RequirementType?.Id == ServiceRequestRequirement.ReturnMoneyId && ReturnMoneyPaymentType?.RefundRequisitesControl == true)
            {
                Requisites.Enabled = true;
            }
            else
            {
                Fio fio = new Fio(_fio);

                Requisites = new RequisitesViewItem()
                {
                    FirstName = _serviceRequestRequisites?.FirstName ?? fio.FirstName,
                    LastName = _serviceRequestRequisites?.LastName ?? fio.LastName,
                    MiddleName = _serviceRequestRequisites?.MiddleName ?? fio.MiddleName,
                    Iban = _serviceRequestRequisites?.Iban,
                    CardNumber = _serviceRequestRequisites?.CardNumber
                };
            }
        }
    }
}