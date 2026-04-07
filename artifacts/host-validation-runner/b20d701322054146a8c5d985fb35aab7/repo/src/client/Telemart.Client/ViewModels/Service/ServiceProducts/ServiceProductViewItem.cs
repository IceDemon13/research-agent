using System;
using System.ComponentModel;
using DevExpress.Mvvm;
using Telemart.Client.Core.Cloning;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects.Warehouse;
using Telemart.Client.ViewModels.Base;
using Telemart.Common.Localization;

namespace Telemart.Client.ViewModels.Service.ServiceProducts
{
    public sealed class ServiceProductViewItem : BindableBase, ILockableEntity, ICommentEntity, IDataErrorInfo, ICloneable, ILocalіzableEntity
    {
        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public int ServiceRequestId
        {
            get { return GetProperty(() => ServiceRequestId); }
            set { SetProperty(() => ServiceRequestId, value); }
        }

        public decimal? PurchasedPrice
        {
            get { return GetProperty(() => PurchasedPrice); }
            set { SetProperty(() => PurchasedPrice, value); }
        }

        public int? PurchasedCurrencyId
        {
            get { return GetProperty(() => PurchasedCurrencyId); }
            set { SetProperty(() => PurchasedCurrencyId, value); }
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

        public int ProductId
        {
            get { return GetProperty(() => ProductId); }
            set { SetProperty(() => ProductId, value); }
        }

        public string ProductFullName
        {
            get { return GetProperty(() => ProductFullName); }
            set { SetProperty(() => ProductFullName, value, () => RaisePropertyChanged(nameof(DisplayProductName))); }
        }

        public string ProductFullNameUkr
        {
            get { return GetProperty(() => ProductFullNameUkr); }
            set { SetProperty(() => ProductFullNameUkr, value, () => RaisePropertyChanged(nameof(DisplayProductName))); }
        }

        public string ProductFullNameEn
        {
            get { return GetProperty(() => ProductFullNameEn); }
            set { SetProperty(() => ProductFullNameEn, value, () => RaisePropertyChanged(nameof(DisplayProductName))); }
        }

        public string Sn
        {
            get { return GetProperty(() => Sn); }
            set { SetProperty(() => Sn, value); }
        }

        public decimal PriceUsd
        {
            get { return GetProperty(() => PriceUsd); }
            set { SetProperty(() => PriceUsd, value); }
        }

        public int WarehouseId
        {
            get { return GetProperty(() => WarehouseId); }
            set { SetProperty(() => WarehouseId, value); }
        }

        public decimal? LossUsd
        {
            get { return GetProperty(() => LossUsd); }
            set { SetProperty(() => LossUsd, value); }
        }

        public ServiceProductState State
        {
            get { return GetProperty(() => State); }
            set { SetProperty(() => State, value); }
        }

        public int? ProductDiscountId
        {
            get { return GetProperty(() => ProductDiscountId); }
            set { SetProperty(() => ProductDiscountId, value); }
        }

        public int? BitrixId
        {
            get { return GetProperty(() => BitrixId); }
            set { SetProperty(() => BitrixId, value); }
        }

        public int TypeId
        {
            get { return GetProperty(() => TypeId); }
            set { SetProperty(() => TypeId, value); }
        }

        public string ServiceActNumber
        {
            get { return GetProperty(() => ServiceActNumber); }
            set { SetProperty(() => ServiceActNumber, value); }
        }

        public string Document1CIn
        {
            get { return GetProperty(() => Document1CIn); }
            set { SetProperty(() => Document1CIn, value); }
        }

        public string Document1COut
        {
            get { return GetProperty(() => Document1COut); }
            set { SetProperty(() => Document1COut, value); }
        }

        public int? SupplierId
        {
            get { return GetProperty(() => SupplierId); }
            set { SetProperty(() => SupplierId, value); }
        }

        public string Comment
        {
            get { return GetProperty(() => Comment); }
            set { SetProperty(() => Comment, value); }
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

        public int? LastServiceRepairId
        {
            get { return GetProperty(() => LastServiceRepairId); }
            set { SetProperty(() => LastServiceRepairId, value); }
        }

        public int? WarehouseToId
        {
            get { return GetProperty(() => WarehouseToId); }
            set { SetProperty(() => WarehouseToId, value); }
        }

        public string DisplayProductName => this.GetLocalName(LocalizableNameType.Ukr);

        string ILocalіzableEntity.Name => ProductFullName;

        string ILocalіzableEntity.NameUkr => ProductFullNameUkr;

        string ILocalіzableEntity.NameEn => ProductFullNameEn;

        string IDataErrorInfo.Error => string.Empty;

        string IDataErrorInfo.this[string columnName] => IDataErrorInfoHelper.GetErrorText(this, columnName);

        object ICloneable.Clone()
        {
            return Clone();
        }

        public ServiceProductViewItem Clone()
        {
            ServiceProductViewItem item = ReflectionObjectCloner.Clone(this);

            return item;
        }
    }
}