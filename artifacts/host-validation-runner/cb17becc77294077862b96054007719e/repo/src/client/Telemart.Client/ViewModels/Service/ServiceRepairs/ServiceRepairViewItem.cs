using System;
using System.ComponentModel;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Core.Cloning;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.ViewModels.Base;
using Telemart.Common.Localization;

namespace Telemart.Client.ViewModels.Service.ServiceRepairs
{
    public sealed class ServiceRepairViewItem : BindableBase, IDataErrorInfo, ICloneable, ILockableEntity, ICommentEntity, ILocalіzableEntity
    {
        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public ServiceRepairState State
        {
            get { return GetProperty(() => State); }
            set { SetProperty(() => State, value); }
        }

        public int ProductId
        {
            get { return GetProperty(() => ProductId); }
            set { SetProperty(() => ProductId, value); }
        }

        public string ProductName
        {
            get { return GetProperty(() => ProductName); }
            set { SetProperty(() => ProductName, value, () => RaisePropertyChanged(nameof(DisplayProductName))); }
        }

        public string ProductNameUkr
        {
            get { return GetProperty(() => ProductNameUkr); }
            set { SetProperty(() => ProductNameUkr, value, () => RaisePropertyChanged(nameof(DisplayProductName))); }
        }

        public string ProductNameEn
        {
            get { return GetProperty(() => ProductNameEn); }
            set { SetProperty(() => ProductNameEn, value, () => RaisePropertyChanged(nameof(DisplayProductName))); }
        }

        public string SerialNumber
        {
            get { return GetProperty(() => SerialNumber); }
            set { SetProperty(() => SerialNumber, value); }
        }

        public string Defect
        {
            get { return GetProperty(() => Defect); }
            set { SetProperty(() => Defect, value); }
        }

        public string RepairInvoice
        {
            get { return GetProperty(() => RepairInvoice); }
            set { SetProperty(() => RepairInvoice, value); }
        }

        public string Comment
        {
            get { return GetProperty(() => Comment); }
            set { SetProperty(() => Comment, value); }
        }

        public string ServiceCenterConclusion
        {
            get { return GetProperty(() => ServiceCenterConclusion); }
            set { SetProperty(() => ServiceCenterConclusion, value); }
        }

        public int? ServiceInvoiceId
        {
            get { return GetProperty(() => ServiceInvoiceId); }
            set { SetProperty(() => ServiceInvoiceId, value); }
        }

        public int ServiceRequestId
        {
            get { return GetProperty(() => ServiceRequestId); }
            set { SetProperty(() => ServiceRequestId, value); }
        }

        public int ServiceRequestOrderId
        {
            get { return GetProperty(() => ServiceRequestOrderId); }
            set { SetProperty(() => ServiceRequestOrderId, value); }
        }

        public int? ServiceCenterId
        {
            get { return GetProperty(() => ServiceCenterId); }
            set { SetProperty(() => ServiceCenterId, value); }
        }

        public int? ServiceRequestLocationId
        {
            get { return GetProperty(() => ServiceRequestLocationId); }
            set { SetProperty(() => ServiceRequestLocationId, value); }
        }

        public Subdivision ServiceRequestSubdivision
        {
            get { return GetProperty(() => ServiceRequestSubdivision); }
            set { SetProperty(() => ServiceRequestSubdivision, value); }
        }

        public int? ServiceRequestWarehouseLocationId
        {
            get { return GetProperty(() => ServiceRequestWarehouseLocationId); }
            set { SetProperty(() => ServiceRequestWarehouseLocationId, value); }
        }

        public DateTime? ServiceRequestReceivedOn
        {
            get { return GetProperty(() => ServiceRequestReceivedOn); }
            set { SetProperty(() => ServiceRequestReceivedOn, value); }
        }

        public int? ServiceRequestPurchasedFrom
        {
            get { return GetProperty(() => ServiceRequestPurchasedFrom); }
            set { SetProperty(() => ServiceRequestPurchasedFrom, value); }
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

        public DateTime? CompletedOn
        {
            get { return GetProperty(() => CompletedOn); }
            set { SetProperty(() => CompletedOn, value); }
        }

        public int? CompletedBy
        {
            get { return GetProperty(() => CompletedBy); }
            set { SetProperty(() => CompletedBy, value); }
        }

        public string Name => ProductName;

        public string NameUkr => ProductNameUkr;

        public string NameEn => ProductNameEn;

        public string DisplayProductName => this.GetLocalName(LocalizableNameType.Ukr);

        #region IDataErrorInfo

        string IDataErrorInfo.Error => string.Empty;

        string IDataErrorInfo.this[string columnName] => IDataErrorInfoHelper.GetErrorText(this, columnName);

        #endregion

        public static void BuildMetadata(MetadataBuilder<ServiceRepairViewItem> builder)
        {
            builder.Property(x => x.Defect)
                .MatchesInstanceRule((x, _) => !string.IsNullOrWhiteSpace(x), () => Resources.RequiredErrorMessage);
        }

        object ICloneable.Clone()
        {
            return Clone();
        }

        public ServiceRepairViewItem Clone()
        {
            return ReflectionObjectCloner.Clone(this);
        }
    }
}