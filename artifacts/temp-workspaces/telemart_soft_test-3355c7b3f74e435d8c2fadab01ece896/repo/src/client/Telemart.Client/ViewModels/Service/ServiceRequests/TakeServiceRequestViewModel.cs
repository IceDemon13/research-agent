using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Telemart.Client.Business.Order;
using Telemart.Client.Business.ServiceRequest;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Core.Extensions;
using Telemart.Client.Data.Requests.Features.AssembledComputer;
using Telemart.Client.Data.Requests.Features.Products;
using Telemart.Client.Data.Requests.Features.ServiceRequest.Actions;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Warehouse;
using Telemart.Client.ViewModels.AssemblyService;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Validation;

namespace Telemart.Client.ViewModels.Service.ServiceRequests
{
    internal sealed class TakeServiceRequestViewModel : TelemartDialogViewModelBase
    {
        private ServiceRequestViewItem serviceRequest;
        private bool _nomenclatureSeriesAccounting;

        public TakeServiceRequestViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IOrderRules orderRules)
            : base(webClient, dictionaries, messageFacadeService)
        {
            OrderRules = orderRules;
        }

        public TakeServiceRequestViewModel()
        {
        }

        #region INPC

        public string Appearance
        {
            get { return GetProperty(() => Appearance); }
            set { SetProperty(() => Appearance, value); }
        }

        public string AppearanceKind
        {
            get { return GetProperty(() => AppearanceKind); }
            set { SetProperty(() => AppearanceKind, value, () => { RaisePropertyChanged(nameof(Appearance)); }); }
        }

        public string InspectionKind
        {
            get { return GetProperty(() => InspectionKind); }
            set { SetProperty(() => InspectionKind, value, InspectionKindChanged); }
        }

        public string InspectionDefect
        {
            get { return GetProperty(() => InspectionDefect); }
            set { SetProperty(() => InspectionDefect, value); }
        }

        public string Comment
        {
            get { return GetProperty(() => Comment); }
            set { SetProperty(() => Comment, value); }
        }

        public string CompletenessComment
        {
            get { return GetProperty(() => CompletenessComment); }
            set { SetProperty(() => CompletenessComment, value); }
        }

        public bool RepairDaysVisible
        {
            get { return GetProperty(() => RepairDaysVisible); }
            set { SetProperty(() => RepairDaysVisible, value); }
        }

        public ServiceRequestRequirement Requirement => serviceRequest.Requirement;

        public int? RepairDays
        {
            get { return GetProperty(() => RepairDays); }
            set { SetProperty(() => RepairDays, value, OnRepairDaysChanged); }
        }

        public string RequirementSummary => serviceRequest.RequirementSummary;

        public string RequirementComment
        {
            get { return GetProperty(() => RequirementComment); }
            set { SetProperty(() => RequirementComment, value); }
        }

        public List<ComboBoxItem> ProductAdditionalServices
        {
            get { return GetProperty(() => ProductAdditionalServices); }
            set { SetProperty(() => ProductAdditionalServices, value); }
        }

        public bool PaidRepair
        {
            get { return GetProperty(() => PaidRepair); }
            set { SetProperty(() => PaidRepair, value); }
        }

        public int? PaymentId
        {
            get { return GetProperty(() => PaymentId); }
            set { SetProperty(() => PaymentId, value); }
        }

        public ObservableCollection<int> SelectedProductAdditionalServiceIds
        {
            get { return GetProperty(() => SelectedProductAdditionalServiceIds); }
            set { SetProperty(() => SelectedProductAdditionalServiceIds, value); }
        }

        public ReadOnlyObservableCollection<Payment> Payments
        {
            get { return GetProperty(() => Payments); }
            private set { SetProperty(() => Payments, value); }
        }

        public ReadOnlyObservableCollection<RepairDays> RepairDaysCollection
        {
            get { return GetProperty(() => RepairDaysCollection); }
            private set { SetProperty(() => RepairDaysCollection, value); }
        }

        public string SerialNumber
        {
            get { return GetProperty(() => SerialNumber); }
            set { SetProperty(() => SerialNumber, value); }
        }

        public bool KeepSerial
        {
            get { return GetProperty(() => KeepSerial); }
            private set { SetProperty(() => KeepSerial, value, () => { RaisePropertiesChanged(nameof(SerialNumber), nameof(SerialNumberNullText)); }); }
        }

        public List<ProductSnLengthDto> ProductSerialNumberLength
        {
            get { return GetProperty(() => ProductSerialNumberLength); }
            private set { SetProperty(() => ProductSerialNumberLength, value, () => { RaisePropertyChanged(nameof(SerialNumber)); }); }
        }

        public int? WarehouseLocationId
        {
            get { return GetProperty(() => WarehouseLocationId); }
            set { SetProperty(() => WarehouseLocationId, value); }
        }

        public ReadOnlyObservableCollection<ValidatableItem> Warehouses
        {
            get { return GetProperty(() => Warehouses); }
            private set { SetProperty(() => Warehouses, value); }
        }

        public ReadOnlyObservableCollection<string> Appearances
        {
            get { return GetProperty(() => Appearances); }
            private set { SetProperty(() => Appearances, value); }
        }

        public ReadOnlyObservableCollection<string> Inspections
        {
            get { return GetProperty(() => Inspections); }
            private set { SetProperty(() => Inspections, value); }
        }

        public string SerialNumberNullText => KeepSerial ? string.Empty : $"SR-{serviceRequest.Id}";

        public bool InspectionDefectEnabled => InspectionKind == ServiceRequestInspection.DefectConfirmed.Name;

        #endregion

        private IOrderRules OrderRules { get; }

        public static void BuildMetadata(MetadataBuilder<TakeServiceRequestViewModel> builder)
        {
            builder.Property(x => x.AppearanceKind)
                .Required(() => Resources.RequiredErrorMessage);

            builder.Property(x => x.Appearance)
                .MatchesInstanceRule((x, y) => y.AppearanceKind != ServiceRequestAppearance.LooksLikeUsed.Name || !string.IsNullOrWhiteSpace(x), () => Resources.RequiredErrorMessage);

            builder.Property(x => x.InspectionDefect)
                .MaxLength(220, () => "Максимальная длина дефекта при осмотре 220 символов")
                .MatchesInstanceRule((x, y) => !(string.IsNullOrWhiteSpace(x) && y.InspectionDefectEnabled), () => Resources.RequiredErrorMessage);

            builder.Property(x => x.CompletenessComment)
                .MatchesInstanceRule((x, y) => !string.IsNullOrWhiteSpace(x), () => Resources.RequiredErrorMessage);

            builder.Property(x => x.PaymentId)
                .MatchesInstanceRule((x, y) => x.HasValue || y.PaidRepair == false, () => Resources.RequiredErrorMessage);

            builder.Property(x => x.SelectedProductAdditionalServiceIds)
                .MatchesInstanceRule((x, y) => x?.Any() == true || y.PaidRepair == false, () => Resources.RequiredErrorMessage);

            builder.Property(x => x.SerialNumber)
                .MatchesInstanceRule((x, y) => (!y.KeepSerial && !y._nomenclatureSeriesAccounting) || !string.IsNullOrWhiteSpace(x), () => Resources.RequiredErrorMessage);

            builder.Property(x => x.WarehouseLocationId)
                .Required(() => Resources.RequiredErrorMessage);

            builder.Property(x => x.RepairDays)
                .MatchesInstanceRule((x, y) => !(y.serviceRequest?.ServiceRepairTypeId == ServiceRepairType.Warranty.Id && x is null), () => Resources.RequiredErrorMessage);
        }

        protected override async Task HandleLoadedAsync()
        {
            serviceRequest = (ServiceRequestViewItem)Parameter;

            await RefreshWarehousesAsync();

            if (!string.IsNullOrWhiteSpace(serviceRequest.SerialNumber))
            {
                AssembledComputersFilteringItem assembledComputersFilteringItem = new AssembledComputersFilteringItem
                {
                    NomenclatureSeries = serviceRequest.SerialNumber
                };

                List<AssembledComputerDto> assembledComputers = await WebClient.ExecuteApiRequestAsync(new QueryAssembledComputers(assembledComputersFilteringItem));

                _nomenclatureSeriesAccounting = assembledComputers.Any(x => x.ProductId.HasValue && x.NomenclatureSeriesAccounting);
            }

            ProductSerialNumberLength = serviceRequest.ProductSerialNumberLength;

            Appearances = Dictionaries.GetItems<ServiceRequestAppearance>().Select(x => x.Name).ToReadOnlyObservableCollection();
            Inspections = Dictionaries.GetItems<ServiceRequestInspection>().Select(x => x.Name).ToReadOnlyObservableCollection();

            InspectionKind = Inspections.First();

            RepairDaysVisible = serviceRequest.ServiceRepairTypeId == ServiceRepairType.Warranty.Id;

            RepairDaysCollection = Dictionaries.GetItems<RepairDays>().ToReadOnlyObservableCollection();

            string appearanceKind = null;
            string appearance = null;

            if (serviceRequest.Appearance != null)
            {
                string[] appearanceParts = serviceRequest.Appearance.Split(new[] { '(', ')' }, StringSplitOptions.RemoveEmptyEntries);

                if (appearanceParts.Length == 2)
                {
                    appearance = appearanceParts[1];
                }

                appearanceKind = Appearances.FirstOrDefault(x => x == appearanceParts[0].Trim());

                if (appearanceKind == null && appearance == null)
                {
                    appearance = appearanceParts[0];
                }
            }

            if (serviceRequest.ServiceRepairTypeId == ServiceRepairType.Paid.Id)
            {
                List<ProductSimpleDto> products = await WebClient.ExecuteApiRequestAsync(new QueryProductServices());

                ProductAdditionalServices = products
                    .Where(x => x.Active > 0 && x.Prices?.Any() == true)
                    .Select(x => new ComboBoxItem(x.Id, $"{x.Name} ({x.Prices.FirstOrDefault(z => z.PriceTypeId == ProductPriceKind.Telemart1)?.Price ?? 0} грн)"))
                    .ToList();

                Payments = Dictionaries.GetItems<Payment>()
                    .Where(x => x.Id == Payment.CashId || x.Id == Payment.BankId)
                    .ToReadOnlyObservableCollection();

                PaidRepair = true;
                RaisePropertiesChanged(nameof(PaymentId), nameof(SelectedProductAdditionalServiceIds));
            }

            KeepSerial = serviceRequest.ProductKeepSerial;
            Comment = serviceRequest.Comment;
            AppearanceKind = appearanceKind;
            Appearance = appearance;
            CompletenessComment = serviceRequest.CompletenessComment;
            WarehouseLocationId = serviceRequest.Location == ServiceRequestLocation.WarehouseId
                ? serviceRequest.WarehouseLocationId
                : null;

            SerialNumber = WebClient.AuthenticatedEmployee.Id == serviceRequest.CreatedBy && WebClient.IsOperationAllowed(BusinessOperation.ServiceRequestSnTake)
                ? serviceRequest.SerialNumber
                : null;

            Title = "Принять заявку";
        }

        protected override async Task HandleOkAsync()
        {
            if (IDataErrorInfoHelper.HasErrors(this))
            {
                return;
            }

            if (KeepSerial)
            {
                string error = OrderRules.ValidateSerialNumber(SerialNumber, ProductSerialNumberLength);

                if (!string.IsNullOrEmpty(error))
                {
                    MessageFacadeService.ShowNotificationError(error);
                    return;
                }
            }

            try
            {
                string requirementText;
                string requirementWarning;

                if (string.IsNullOrWhiteSpace(RequirementComment))
                {
                    requirementText = serviceRequest.RequirementText;
                    requirementWarning = RequirementSummary;
                }
                else
                {
                    requirementText = $"{serviceRequest.RequirementText} Комментарий: {RequirementComment}";
                    requirementWarning = $"{RequirementSummary}. {RequirementComment}";
                }

                if (!MessageFacadeService.Confirm($"Вы подтверждаете, что требование клиента: {requirementWarning}?"))
                {
                    return;
                }

                ServiceRequestTakeDto dto = new ServiceRequestTakeDto
                {
                    Id = serviceRequest.Id,
                    Appearance = AppearanceKind.JoinSmart(Appearance),
                    Inspection = InspectionKind.JoinSmart(InspectionDefect),
                    SerialNumber = SerialNumber,
                    CompletenessComment = CompletenessComment,
                    RequirementText = requirementText,
                    RepairDays = Requirement == ServiceRequestRequirement.Repair ? RepairDays : null,
                    WarehouseLocationId = WarehouseLocationId!.Value,
                    Comment = Comment,
                    ProductAdditionalServiceIds = SelectedProductAdditionalServiceIds?.ToList(),
                    PaymentId = PaymentId
                };

                TakeServiceRequest gatewayRequest = new TakeServiceRequest(serviceRequest.Id, dto);

                await WebClient.ExecuteApiRequestAsync(gatewayRequest);

                MessageFacadeService.ShowNotificationInfo($"Заявка №{serviceRequest.Id} успешно принята");

                IsOk = true;
                Close();
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при принятии заявки");
                ShowValidationResultView("Ошибки при принятии заявки", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to take service request");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при принятии заявки");
                Logger.LogError(exception, "Failed to take service request");
            }
        }

        private void InspectionKindChanged()
        {
            RaisePropertiesChanged(nameof(InspectionDefect), nameof(InspectionDefectEnabled));

            if (InspectionKind != ServiceRequestInspection.DefectConfirmed.Name)
            {
                InspectionDefect = null;
            }
        }

        private void OnRepairDaysChanged()
        {
            if (Requirement == ServiceRequestRequirement.Repair)
            {
                ServiceRequestRequirementBuilder requirementBuilder = new ServiceRequestRequirementBuilder(
                    ServiceRequestRequirement.Repair,
                    RepairDays,
                    serviceRequest.ServiceRepairTypeId,
                    null,
                    null,
                    null,
                    null);

                serviceRequest.RequirementText = requirementBuilder.GetRequirementText();

                RaisePropertyChanged(nameof(RequirementSummary));
            }
        }

        private async Task RefreshWarehousesAsync()
        {
            List<WarehouseDto> warehouses = await WebClient.ExecuteApiRequestAsync(new QueryWarehouses(), true).GetPagedResultDataAsync();

            Warehouses = warehouses
                .Where(x => x.Active == 1 && WebClient.AuthenticatedEmployee.AllowWarehouses.Contains(x.Id))
                .OrderBy(x => x.Name)
                .Select(x => new ValidatableItem { Id = x.Id, Name = x.Name })
                .ToReadOnlyObservableCollection();
        }
    }
}