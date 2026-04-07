using System.Collections.ObjectModel;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Common.Validation;
using Telemart.Client.Dictionaries;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Common;
using Telemart.Client.ViewModels.Validation;

namespace Telemart.Client.ViewModels.Service.ServiceRequests.Diagnostics
{
    public sealed class DiagnoseServiceRequestModel : TelemartViewItemBase
    {
        #region Info

        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public ServiceRequestRequirement Requirement
        {
            get { return GetProperty(() => Requirement); }
            set { SetProperty(() => Requirement, value); }
        }

        public int? RequirementPaymentId
        {
            get { return GetProperty(() => RequirementPaymentId); }
            set { SetProperty(() => RequirementPaymentId, value); }
        }

        public string RequirementSummary
        {
            get { return GetProperty(() => RequirementSummary); }
            set { SetProperty(() => RequirementSummary, value); }
        }

        public string ProductName
        {
            get { return GetProperty(() => ProductName); }
            set { SetProperty(() => ProductName, value); }
        }

        public string Appearance
        {
            get { return GetProperty(() => Appearance); }
            set { SetProperty(() => Appearance, value); }
        }

        public int BonusAmount
        {
            get { return GetProperty(() => BonusAmount); }
            set { SetProperty(() => BonusAmount, value); }
        }

        public bool ShowBonusAmount
        {
            get { return GetProperty(() => ShowBonusAmount); }
            set { SetProperty(() => ShowBonusAmount, value); }
        }

        public string StatedDefect
        {
            get { return GetProperty(() => StatedDefect); }
            set { SetProperty(() => StatedDefect, value); }
        }

        public string CompletenessComment
        {
            get { return GetProperty(() => CompletenessComment); }
            set { SetProperty(() => CompletenessComment, value); }
        }

        public int? RejectReasonId
        {
            get { return GetProperty(() => RejectReasonId); }
            set { SetProperty(() => RejectReasonId, value); }
        }

        public int OrderId
        {
            get { return GetProperty(() => OrderId); }
            set { SetProperty(() => OrderId, value); }
        }

        public int ContractorId
        {
            get { return GetProperty(() => ContractorId); }
            set { SetProperty(() => ContractorId, value); }
        }

        public int ProductId
        {
            get { return GetProperty(() => ProductId); }
            set { SetProperty(() => ProductId, value); }
        }

        public RequisitesViewItem Requisites
        {
            get { return GetProperty(() => Requisites); }
            set { SetProperty(() => Requisites, value); }
        }

        #endregion

        #region Reject

        public string RejectReason
        {
            get { return GetProperty(() => RejectReason); }
            set { SetProperty(() => RejectReason, value); }
        }

        public string RejectAlternative
        {
            get { return GetProperty(() => RejectAlternative); }
            set { SetProperty(() => RejectAlternative, value); }
        }

        #endregion

        #region Return

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

        public ReadOnlyObservableCollection<ServiceRequestCompensationPrice> CompensationPrices
        {
            get { return GetProperty(() => CompensationPrices); }
            set { SetProperty(() => CompensationPrices, value); }
        }

        public ServiceRequestCompensationPrice CompensationPrice
        {
            get { return GetProperty(() => CompensationPrice); }
            set { SetProperty(() => CompensationPrice, value, CompensationPriceChanged); }
        }

        #endregion

        #region NomenclatureSeries

        public AssembledComputerSaveDto AssembledComputerSaveDto
        {
            get { return GetProperty(() => AssembledComputerSaveDto); }
            set { SetProperty(() => AssembledComputerSaveDto, value); }
        }

        public string SerialNumber
        {
            get { return GetProperty(() => SerialNumber); }
            set { SetProperty(() => SerialNumber, value); }
        }

        public bool NomenclatureSeriesAccounting
        {
            get { return GetProperty(() => NomenclatureSeriesAccounting); }
            set { SetProperty(() => NomenclatureSeriesAccounting, value); }
        }

        #endregion

        public ServiceRequestResolution Resolution
        {
            get { return GetProperty(() => Resolution); }
            set { SetProperty(() => Resolution, value, () => RaisePropertiesChanged(nameof(IsRejectReasonVisible), nameof(RejectReasonId))); }
        }

        public int? ServiceCenterId
        {
            get { return GetProperty(() => ServiceCenterId); }
            set { SetProperty(() => ServiceCenterId, value); }
        }

        public ServiceRequestDto Result
        {
            get { return GetProperty(() => Result); }
            set { SetProperty(() => Result, value); }
        }

        public ReadOnlyObservableCollection<ValidationResultItem> ValidationItems
        {
            get { return GetProperty(() => ValidationItems); }
            set { SetProperty(() => ValidationItems, value); }
        }

        public ReadOnlyObservableCollection<ValidationResultItem> Warnings
        {
            get { return GetProperty(() => Warnings); }
            set { SetProperty(() => Warnings, value); }
        }

        public bool IsRejectReasonVisible => Resolution?.Id == ServiceRequestResolution.RejectedId;

        public static void BuildMetadata(MetadataBuilder<DiagnoseServiceRequestModel> builder)
        {
            builder.Property(x => x.Resolution).Required(() => Resources.RequiredErrorMessage);

            builder.Property(x => x.RejectReason).MatchesRule(x => !string.IsNullOrWhiteSpace(x), () => Resources.RequiredErrorMessage);
            builder.Property(x => x.RejectAlternative).MatchesRule(x => !string.IsNullOrWhiteSpace(x), () => Resources.RequiredErrorMessage);

            builder.Property(x => x.Amount).MatchesRule(x => x > 0, () => Resources.RequiredErrorMessage).MaxProductPrice();
            builder.Property(x => x.CompensationPrices).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.CompensationPrice).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.RejectReasonId)
                .MatchesInstanceRule((x, y) => !y.IsRejectReasonVisible || x.HasValue, () => Resources.RequiredErrorMessage);
            builder.Property(x => x.BonusAmount)
                .MatchesInstanceRule((x, y) => x > 0 || y.Requirement != ServiceRequestRequirement.TradeIn, () => "Значение должно быть больше 0");
        }

        private void CompensationPriceChanged()
        {
            Amount = CompensationPrice?.Value;
        }
    }
}