using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using Newtonsoft.Json;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Core.Cloning;
using Telemart.Client.Core.Extensions;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Common;
using Telemart.Common.Localization;

namespace Telemart.Client.ViewModels.Service.ServiceRequests
{
    public sealed class ServiceRequestViewItem : BindableBase, IDataErrorInfo, ICloneable, ILockableEntity, ICommentEntity, ILocalіzableEntity
    {
        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public int ContractorId
        {
            get { return GetProperty(() => ContractorId); }
            set { SetProperty(() => ContractorId, value); }
        }

        public int SubdivisionId
        {
            get { return GetProperty(() => SubdivisionId); }
            set { SetProperty(() => SubdivisionId, value); }
        }

        public int? LegalEntityId
        {
            get { return GetProperty(() => LegalEntityId); }
            set { SetProperty(() => LegalEntityId, value); }
        }

        public Subdivision Subdivision
        {
            get { return GetProperty(() => Subdivision); }
            set { SetProperty(() => Subdivision, value); }
        }

        public string CustomerStateText
        {
            get { return GetProperty(() => CustomerStateText); }
            set { SetProperty(() => CustomerStateText, value); }
        }

        public bool WarrantyRemoved
        {
            get { return GetProperty(() => WarrantyRemoved); }
            set { SetProperty(() => WarrantyRemoved, value); }
        }

        public string Fio
        {
            get { return GetProperty(() => Fio); }
            set { SetProperty(() => Fio, value); }
        }

        public string Phone
        {
            get { return GetProperty(() => Phone); }
            set { SetProperty(() => Phone, value); }
        }

        public string Phone2
        {
            get { return GetProperty(() => Phone2); }
            set { SetProperty(() => Phone2, value); }
        }

        public string Email
        {
            get { return GetProperty(() => Email); }
            set { SetProperty(() => Email, value); }
        }

        public bool BonusCharged
        {
            get { return GetProperty(() => BonusCharged); }
            set { SetProperty(() => BonusCharged, value); }
        }

        public int ProductId
        {
            get { return GetProperty(() => ProductId); }
            set { SetProperty(() => ProductId, value, () => { RaisePropertyChanged(nameof(OriginalProductDiffersFromCurrent)); }); }
        }

        public int ProductInId
        {
            get { return GetProperty(() => ProductInId); }
            set { SetProperty(() => ProductInId, value, () => { RaisePropertyChanged(nameof(OriginalProductDiffersFromCurrent)); }); }
        }

        public bool? ReadyForRecomplectation
        {
            get { return GetProperty(() => ReadyForRecomplectation); }
            set { SetProperty(() => ReadyForRecomplectation, value); }
        }

        public string ProductName
        {
            get { return GetProperty(() => ProductName); }
            set { SetProperty(() => ProductName, value, () => { RaisePropertiesChanged(nameof(ProductFullName), nameof(DisplayProductName)); }); }
        }

        public string ProductNameUkr
        {
            get { return GetProperty(() => ProductNameUkr); }
            set { SetProperty(() => ProductNameUkr, value, () => { RaisePropertiesChanged(nameof(ProductFullName), nameof(DisplayProductName)); }); }
        }

        public string ProductNameEn
        {
            get { return GetProperty(() => ProductNameEn); }
            set { SetProperty(() => ProductNameEn, value, () => { RaisePropertiesChanged(nameof(ProductFullName), nameof(DisplayProductName)); }); }
        }

        public string ProductPrefixRus
        {
            get { return GetProperty(() => ProductPrefixRus); }
            set { SetProperty(() => ProductPrefixRus, value, () => { RaisePropertyChanged(nameof(ProductFullName)); }); }
        }

        public string ProductPrefixUkr
        {
            get { return GetProperty(() => ProductPrefixUkr); }
            set { SetProperty(() => ProductPrefixUkr, value, () => { RaisePropertyChanged(nameof(ProductFullName)); }); }
        }

        public string ProductPrefixEn
        {
            get { return GetProperty(() => ProductPrefixEn); }
            set { SetProperty(() => ProductPrefixEn, value, () => { RaisePropertyChanged(nameof(ProductFullName)); }); }
        }

        public bool ProductKeepSerial
        {
            get { return GetProperty(() => ProductKeepSerial); }
            set { SetProperty(() => ProductKeepSerial, value); }
        }

        public int? ServiceRepairTypeId
        {
            get { return GetProperty(() => ServiceRepairTypeId); }
            set { SetProperty(() => ServiceRepairTypeId, value); }
        }

        public List<ProductSnLengthDto> ProductSerialNumberLength
        {
            get { return GetProperty(() => ProductSerialNumberLength); }
            set { SetProperty(() => ProductSerialNumberLength, value); }
        }

        public int? CustomerId
        {
            get { return GetProperty(() => CustomerId); }
            set { SetProperty(() => CustomerId, value); }
        }

        public int OrderId
        {
            get { return GetProperty(() => OrderId); }
            set { SetProperty(() => OrderId, value); }
        }

        public int OrderPaymentTypeId
        {
            get { return GetProperty(() => OrderPaymentTypeId); }
            set { SetProperty(() => OrderPaymentTypeId, value); }
        }

        public int? RejectReasonId
        {
            get { return GetProperty(() => RejectReasonId); }
            set { SetProperty(() => RejectReasonId, value); }
        }

        public int StateId
        {
            get { return GetProperty(() => StateId); }
            set { SetProperty(() => StateId, value, () => RaisePropertiesChanged(nameof(State), nameof(Appearance), nameof(CompletenessComment), nameof(TtnIn))); }
        }

        public ServiceRequestState State
        {
            get { return GetProperty(() => State); }
            set { SetProperty(() => State, value, () => RaisePropertiesChanged(nameof(IsCompleted), nameof(InProgress), nameof(Appearance), nameof(CompletenessComment), nameof(TtnIn))); }
        }

        public ServiceRequestRequirement Requirement
        {
            get { return GetProperty(() => Requirement); }
            set { SetProperty(() => Requirement, value, () => RaisePropertiesChanged(nameof(ExchangeOn), nameof(RequirementSummary))); }
        }

        public decimal? TradeInBuyoutAmount
        {
            get { return GetProperty(() => TradeInBuyoutAmount); }
            set { SetProperty(() => TradeInBuyoutAmount, value); }
        }

        public int? TradeInId
        {
            get { return GetProperty(() => TradeInId); }
            set { SetProperty(() => TradeInId, value); }
        }

        public ComboBoxItem? Group
        {
            get { return GetProperty(() => Group); }
            set { SetProperty(() => Group, value); }
        }

        public int? BundleId
        {
            get { return GetProperty(() => BundleId); }
            set { SetProperty(() => BundleId, value); }
        }

        public ServiceRequestResolution RequirementResolution
        {
            get { return GetProperty(() => RequirementResolution); }
            set { SetProperty(() => RequirementResolution, value, () => RaisePropertyChanged(nameof(RequirementResolutionSummary))); }
        }

        public int? RequirementPaymentId
        {
            get { return GetProperty(() => RequirementPaymentId); }
            set { SetProperty(() => RequirementPaymentId, value); }
        }

        public string RequirementText
        {
            get { return GetProperty(() => RequirementText); }
            set { SetProperty(() => RequirementText, value, () => RaisePropertyChanged(nameof(RequirementSummary))); }
        }

        public string RequirementResolutionText
        {
            get { return GetProperty(() => RequirementResolutionText); }
            set { SetProperty(() => RequirementResolutionText, value, () => RaisePropertyChanged(nameof(RequirementResolutionSummary))); }
        }

        public string SerialNumber
        {
            get { return GetProperty(() => SerialNumber); }
            set { SetProperty(() => SerialNumber, value); }
        }

        public string StatedDefect
        {
            get { return GetProperty(() => StatedDefect); }
            set { SetProperty(() => StatedDefect, value); }
        }

        public string Appearance
        {
            get { return GetProperty(() => Appearance); }
            set { SetProperty(() => Appearance, value); }
        }

        public int? CompletenessId
        {
            get { return GetProperty(() => CompletenessId); }
            set { SetProperty(() => CompletenessId, value, () => RaisePropertyChanged(nameof(CompletenessComment))); }
        }

        public string CompletenessComment
        {
            get { return GetProperty(() => CompletenessComment); }
            set { SetProperty(() => CompletenessComment, value); }
        }

        public string ExchangeOn
        {
            get { return GetProperty(() => ExchangeOn); }
            set { SetProperty(() => ExchangeOn, value); }
        }

        public string ExchangeFund
        {
            get { return GetProperty(() => ExchangeFund); }
            set { SetProperty(() => ExchangeFund, value); }
        }

        public int SourceId
        {
            get { return GetProperty(() => SourceId); }
            set { SetProperty(() => SourceId, value); }
        }

        public int? EmployeeLockId
        {
            get { return GetProperty(() => EmployeeLockId); }
            set { SetProperty(() => EmployeeLockId, value); }
        }

        public string EmployeeLockName
        {
            get { return GetProperty(() => EmployeeLockName); }
            set { SetProperty(() => EmployeeLockName, value); }
        }

        public string MovedTo
        {
            get { return GetProperty(() => MovedTo); }
            set { SetProperty(() => MovedTo, value); }
        }

        public string Returned
        {
            get { return GetProperty(() => Returned); }
            set { SetProperty(() => Returned, value); }
        }

        public string Refund
        {
            get { return GetProperty(() => Refund); }
            set { SetProperty(() => Refund, value); }
        }

        public string Comment
        {
            get { return GetProperty(() => Comment); }
            set { SetProperty(() => Comment, value); }
        }

        public int? InvoiceId
        {
            get { return GetProperty(() => InvoiceId); }
            set { SetProperty(() => InvoiceId, value); }
        }

        public int[] ServiceProductIds
        {
            get { return GetProperty(() => ServiceProductIds); }
            set { SetProperty(() => ServiceProductIds, value, ServiceProductIdsChanged); }
        }

        public int? SelectedServiceProductId
        {
            get { return GetProperty(() => SelectedServiceProductId); }
            set { SetProperty(() => SelectedServiceProductId, value); }
        }

        public int? SelectedRefundId
        {
            get { return GetProperty(() => SelectedRefundId); }
            set { SetProperty(() => SelectedRefundId, value); }
        }

        public int? PurchasedFromContractorId
        {
            get { return GetProperty(() => PurchasedFromContractorId); }
            set { SetProperty(() => PurchasedFromContractorId, value); }
        }

        public DateTime? PurchasedOn
        {
            get { return GetProperty(() => PurchasedOn); }
            set { SetProperty(() => PurchasedOn, value); }
        }

        public int? ProductNewId
        {
            get { return GetProperty(() => ProductNewId); }
            set { SetProperty(() => ProductNewId, value); }
        }

        public int? OrderNewId
        {
            get { return GetProperty(() => OrderNewId); }
            set { SetProperty(() => OrderNewId, value); }
        }

        public int? CityId
        {
            get { return GetProperty(() => CityId); }
            set { SetProperty(() => CityId, value); }
        }

        public int? WarehouseInId
        {
            get { return GetProperty(() => WarehouseInId); }
            set { SetProperty(() => WarehouseInId, value); }
        }

        public CarryType CarryIn
        {
            get { return GetProperty(() => CarryIn); }
            set { SetProperty(() => CarryIn, value); }
        }

        public CarryType CarryOut
        {
            get { return GetProperty(() => CarryOut); }
            set { SetProperty(() => CarryOut, value, CarryOutChanged); }
        }

        public DeliveryDataDto DeliveryDataOut
        {
            get { return GetProperty(() => DeliveryDataOut); }
            set { SetProperty(() => DeliveryDataOut, value); }
        }

        public string TtnIn
        {
            get { return GetProperty(() => TtnIn); }
            set { SetProperty(() => TtnIn, value); }
        }

        public string SendTo
        {
            get { return GetProperty(() => SendTo); }
            set { SetProperty(() => SendTo, value); }
        }

        public string TtnOut
        {
            get { return GetProperty(() => TtnOut); }
            set { SetProperty(() => TtnOut, value); }
        }

        public int? Location
        {
            get { return GetProperty(() => Location); }
            set { SetProperty(() => Location, value, () => { RaisePropertiesChanged(nameof(WarehouseLocationId), nameof(ServiceRepairId)); }); }
        }

        public string LocationText
        {
            get { return GetProperty(() => LocationText); }
            set { SetProperty(() => LocationText, value); }
        }

        public int? WarehouseLocationId
        {
            get { return GetProperty(() => WarehouseLocationId); }
            set { SetProperty(() => WarehouseLocationId, value); }
        }

        public int? ServiceRepairId
        {
            get { return GetProperty(() => ServiceRepairId); }
            set { SetProperty(() => ServiceRepairId, value); }
        }

        public string ServiceActNumber
        {
            get { return GetProperty(() => ServiceActNumber); }
            set { SetProperty(() => ServiceActNumber, value); }
        }

        public string Inspection
        {
            get { return GetProperty(() => Inspection); }
            set { SetProperty(() => Inspection, value); }
        }

        public List<int> RefundIds
        {
            get { return GetProperty(() => RefundIds); }
            set { SetProperty(() => RefundIds, value, RefundIdsChanged); }
        }

        public DateTime? ReceivedOn
        {
            get { return GetProperty(() => ReceivedOn); }
            set { SetProperty(() => ReceivedOn, value); }
        }

        public int? ReceivedBy
        {
            get { return GetProperty(() => ReceivedBy); }
            set { SetProperty(() => ReceivedBy, value); }
        }

        public DateTime? ReadyOn
        {
            get { return GetProperty(() => ReadyOn); }
            set { SetProperty(() => ReadyOn, value); }
        }

        public DateTime? DiagnosticOn
        {
            get { return GetProperty(() => DiagnosticOn); }
            set { SetProperty(() => DiagnosticOn, value); }
        }

        public int? DiagnosticBy
        {
            get { return GetProperty(() => DiagnosticBy); }
            set { SetProperty(() => DiagnosticBy, value); }
        }

        public DateTime ModifiedOn
        {
            get { return GetProperty(() => ModifiedOn); }
            set { SetProperty(() => ModifiedOn, value); }
        }

        public int ModifiedBy
        {
            get { return GetProperty(() => ModifiedBy); }
            set { SetProperty(() => ModifiedBy, value); }
        }

        public DateTime CreatedOn
        {
            get { return GetProperty(() => CreatedOn); }
            set { SetProperty(() => CreatedOn, value); }
        }

        public int CreatedBy
        {
            get { return GetProperty(() => CreatedBy); }
            set { SetProperty(() => CreatedBy, value); }
        }

        public string PurchasedSummary
        {
            get { return GetProperty(() => PurchasedSummary); }
            set { SetProperty(() => PurchasedSummary, value); }
        }

        public string RequirementSummary => Requirement.Name.JoinSmart(RequirementText);

        public string RequirementResolutionSummary => RequirementResolution?.Name.JoinSmart(RequirementResolutionText);

        public int DiscussionState
        {
            get { return GetProperty(() => DiscussionState); }
            set { SetProperty(() => DiscussionState, value); }
        }

        public DateTime LastActivityOn
        {
            get { return GetProperty(() => LastActivityOn); }
            set { SetProperty(() => LastActivityOn, value); }
        }

        public int? RepairDays
        {
            get { return GetProperty(() => RepairDays); }
            set { SetProperty(() => RepairDays, value); }
        }

        public DateTime? DateX
        {
            get { return GetProperty(() => DateX); }
            set { SetProperty(() => DateX, value); }
        }

        public DateTime OrderCompletedOn
        {
            get { return GetProperty(() => OrderCompletedOn); }
            set { SetProperty(() => OrderCompletedOn, value); }
        }

        public string NpCourierCallBarcode
        {
            get { return GetProperty(() => NpCourierCallBarcode); }
            set { SetProperty(() => NpCourierCallBarcode, value); }
        }

        public string NpCourierCallInterval
        {
            get { return GetProperty(() => NpCourierCallInterval); }
            set { SetProperty(() => NpCourierCallInterval, value); }
        }

        public string ProductFullName => this.GetLocalName(LocalizableNameType.Ukr);

        public int? NewCallsCount
        {
            get { return GetProperty(() => NewCallsCount); }
            set { SetProperty(() => NewCallsCount, value, () => RaisePropertyChanged(nameof(CallsCountString))); }
        }

        public int? CallsCount
        {
            get { return GetProperty(() => CallsCount); }
            set { SetProperty(() => CallsCount, value, () => RaisePropertyChanged(nameof(CallsCountString))); }
        }

        public int? NewComplaintsCount
        {
            get { return GetProperty(() => NewComplaintsCount); }
            set { SetProperty(() => NewComplaintsCount, value, () => RaisePropertyChanged(nameof(ComplaintsCountString))); }
        }

        public int? ComplaintsCount
        {
            get { return GetProperty(() => ComplaintsCount); }
            set { SetProperty(() => ComplaintsCount, value, () => RaisePropertyChanged(nameof(ComplaintsCountString))); }
        }

        public int? DiscussionsCount
        {
            get { return GetProperty(() => DiscussionsCount); }
            set { SetProperty(() => DiscussionsCount, value); }
        }

        public int? DocumentsCount
        {
            get { return GetProperty(() => DocumentsCount); }
            set { SetProperty(() => DocumentsCount, value); }
        }

        public string FiscalId
        {
            get { return GetProperty(() => FiscalId); }
            set { SetProperty(() => FiscalId, value); }
        }

        public bool CompletedOnFiscalRegistrar
        {
            get { return GetProperty(() => CompletedOnFiscalRegistrar); }
            set { SetProperty(() => CompletedOnFiscalRegistrar, value); }
        }

        public RequisitesViewItem Requisites
        {
            get { return GetProperty(() => Requisites); }
            set { SetProperty(() => Requisites, value); }
        }

        public bool RealCompletedOnFiscalRegistrar
        {
            get { return GetProperty(() => RealCompletedOnFiscalRegistrar); }
            set { SetProperty(() => RealCompletedOnFiscalRegistrar, value); }
        }

        public bool CompletedOnMoneyRefund
        {
            get { return GetProperty(() => CompletedOnMoneyRefund); }
            set { SetProperty(() => CompletedOnMoneyRefund, value); }
        }

        public string CallsCountString => $"{NewCallsCount ?? 0}/{CallsCount ?? 0}";

        public string ComplaintsCountString => $"{NewComplaintsCount ?? 0}/{ComplaintsCount ?? 0}";

        public bool IsCompleted => State.CompletedFlag;

        public bool InProgress => State.InProgressFlag;

        public bool OriginalProductDiffersFromCurrent => ProductId != ProductInId;

        public string DisplayProductName => EntityLocalіzerExtensions.GetLacalString(ProductName, ProductNameUkr, ProductNameEn, LocalizableNameType.Ukr);

        string ILocalіzableEntity.Name => ProductName.GetStringWithPrefix(ProductPrefixRus);

        string ILocalіzableEntity.NameUkr => ProductNameUkr.GetStringWithPrefix(ProductPrefixUkr);

        string ILocalіzableEntity.NameEn => ProductNameEn.GetStringWithPrefix(ProductPrefixEn);

        #region IDataErrorInfo

        string IDataErrorInfo.Error => string.Empty;

        string IDataErrorInfo.this[string columnName] => IDataErrorInfoHelper.GetErrorText(this, columnName);

        #endregion

        public static void BuildMetadata(MetadataBuilder<ServiceRequestViewItem> builder)
        {
            builder.Property(x => x.Fio)
                .Required(() => Resources.RequiredErrorMessage)
                .MaxLength(100, () => "Значение поля должно быть короче 100 символов");

            builder.Property(x => x.Phone)
                .Required(() => Resources.RequiredErrorMessage);

            builder.Property(x => x.Email)
                .EmailAddressDataType(() => "Введите корректно e-mail");

            builder.Property(x => x.Comment)
                .MaxLength(1024, () => "Значение поля должно быть короче 1024 символов");

            builder.Property(x => x.StatedDefect)
                .MatchesInstanceRule((x, y) => !string.IsNullOrWhiteSpace(x) || y.Requirement == ServiceRequestRequirement.TradeIn, () => Resources.RequiredErrorMessage);

            builder.Property(x => x.Appearance)
                .MatchesInstanceRule((x, y) => y.StateId == ServiceRequestState.New.Id || !string.IsNullOrWhiteSpace(x), () => Resources.RequiredErrorMessage);

            builder.Property(x => x.CompletenessComment)
                .MatchesInstanceRule((x, y) => y.StateId == ServiceRequestState.New.Id || !string.IsNullOrWhiteSpace(x), () => Resources.RequiredErrorMessage);

            builder.Property(x => x.Requirement)
                .Required(() => Resources.RequiredErrorMessage);

            builder.Property(x => x.WarehouseInId)
                .Required(() => Resources.RequiredErrorMessage);

            builder.Property(x => x.CarryIn)
                .Required(() => Resources.RequiredErrorMessage);

            builder.Property(x => x.CarryOut)
                .Required(() => Resources.RequiredErrorMessage);

            builder.Property(x => x.CityId)
                .Required(() => Resources.RequiredErrorMessage);

            builder.Property(x => x.DeliveryDataOut).MatchesRule(x => x != null, () => Resources.RequiredErrorMessage);

            builder.Property(x => x.TtnIn)
                .MatchesInstanceRule(
                    (x, y) => string.IsNullOrWhiteSpace(y.CarryIn?.TtnRegex)
                        || ((y.StateId == ServiceRequestState.New.Id || y.StateId == ServiceRequestState.Cancelled.Id) && string.IsNullOrEmpty(x))
                        || (x != null && Regex.IsMatch(x, y.CarryIn.TtnRegex)),
                    () => "Введите корректно ТТН поступления");
        }

        object ICloneable.Clone()
        {
            return Clone();
        }

        public ServiceRequestViewItem Clone()
        {
            ServiceRequestViewItem model = ReflectionObjectCloner.Clone(this);

            model.Requisites = ReflectionObjectCloner.Clone(Requisites);

            return model;
        }

        private void ServiceProductIdsChanged()
        {
            if (ServiceProductIds?.Any() == true)
            {
                SelectedServiceProductId = ServiceProductIds.First();
            }
        }

        private void RefundIdsChanged()
        {
            if (RefundIds?.Any() == true)
            {
                SelectedRefundId = RefundIds.First();
            }
        }

        private void CarryOutChanged()
        {
            RaisePropertiesChanged(nameof(DeliveryDataOut), nameof(CityId), nameof(TtnIn), nameof(TtnOut));
        }
    }
}