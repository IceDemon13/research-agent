using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Validation;
using Telemart.Client.Core.Cloning;
using Telemart.Client.Dictionaries;
using Telemart.Client.FiscalRegistrar;
using Telemart.Client.Properties;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Cashbox
{
    public class CashboxViewItem : BindableBase, ILockableEntity, IDataErrorInfo, ICloneable
    {
        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public int? EmployeeLockId
        {
            get { return GetProperty(() => EmployeeLockId); }
            set { SetProperty(() => EmployeeLockId, value); }
        }

        public int? CurrencyId
        {
            get { return GetProperty(() => CurrencyId); }
            set { SetProperty(() => CurrencyId, value); }
        }

        public int? EmployeeId
        {
            get { return GetProperty(() => EmployeeId); }
            set { SetProperty(() => EmployeeId, value); }
        }

        public ComboBoxItem? Employee
        {
            get { return GetProperty(() => Employee); }
            set { SetProperty(() => Employee, value, () => EmployeeId = Employee?.Id); }
        }

        public int? TypeId
        {
            get { return GetProperty(() => TypeId); }
            set { SetProperty(() => TypeId, value, TypeChanged); }
        }

        public int? ConnectTypeId
        {
            get { return GetProperty(() => ConnectTypeId); }
            set { SetProperty(() => ConnectTypeId, value, ConnectTypeChanged); }
        }

        public int? CredentialId
        {
            get { return GetProperty(() => CredentialId); }
            set { SetProperty(() => CredentialId, value, CredentialChanged); }
        }

        public int? LegalEntityId
        {
            get { return GetProperty(() => LegalEntityId); }
            set { SetProperty(() => LegalEntityId, value); }
        }

        public int? StrongboxId
        {
            get { return GetProperty(() => StrongboxId); }
            set { SetProperty(() => StrongboxId, value); }
        }

        public int? WarehouseId
        {
            get { return GetProperty(() => WarehouseId); }
            set { SetProperty(() => WarehouseId, value); }
        }

        public string PaymentAccount
        {
            get { return GetProperty(() => PaymentAccount); }
            set { SetProperty(() => PaymentAccount, value); }
        }

        public string Name
        {
            get { return GetProperty(() => Name); }
            set { SetProperty(() => Name, value); }
        }

        public decimal? Fee
        {
            get { return GetProperty(() => Fee); }
            set { SetProperty(() => Fee, value); }
        }

        public decimal? MinFee
        {
            get { return GetProperty(() => MinFee); }
            set { SetProperty(() => MinFee, value); }
        }

        public decimal? IncombustibleAmount
        {
            get { return GetProperty(() => IncombustibleAmount); }
            set { SetProperty(() => IncombustibleAmount, value); }
        }

        public bool AutoPay
        {
            get { return GetProperty(() => AutoPay); }
            set { SetProperty(() => AutoPay, value); }
        }

        public bool IsActive
        {
            get { return GetProperty(() => IsActive); }
            set { SetProperty(() => IsActive, value, () => RaisePropertyChanged(nameof(Employee))); }
        }

        public string EmployeeLockName
        {
            get { return GetProperty(() => EmployeeLockName); }
            set { SetProperty(() => EmployeeLockName, value); }
        }

        public ObservableCollection<int> AllowedPayments
        {
            get { return GetProperty(() => AllowedPayments); }
            set { SetProperty(() => AllowedPayments, value); }
        }

        public bool IsNotReadonlyPaymentAccount => TypeId == CashboxType.PaymentAccount.Id;

        public bool AllowEditStrongbox => TypeId == CashboxType.FiscalRegistrar.Id;

        public bool AllowEditCredentials => ConnectTypeId == FiscalRegistrarType.Software.Id;

        public bool AllowEditConnectType => TypeId == CashboxType.FiscalRegistrar.Id;

        public bool WarehouseVisible => TypeId == CashboxType.Employee.Id || TypeId == CashboxType.FiscalRegistrar.Id;

        string IDataErrorInfo.Error => string.Empty;

        string IDataErrorInfo.this[string columnName] => IDataErrorInfoHelper.GetErrorText(this, columnName);

        public static void BuildMetadata(MetadataBuilder<CashboxViewItem> builder)
        {
            builder.Property(x => x.Name)
                .Required(() => Resources.RequiredErrorMessage)
                .MaxLength(100, () => "Длина поля должна быть короче 100 символов");
            builder.Property(x => x.TypeId)
                .Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.Employee)
                .RequiredActive(x => x.IsActive);
            builder.Property(x => x.CurrencyId)
                .Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.Fee)
                .Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.MinFee)
                .Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.LegalEntityId)
                .Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.StrongboxId)
                .MatchesInstanceRule((x, y) => !y.AllowEditStrongbox || x.HasValue, () => Resources.RequiredErrorMessage);
            builder.Property(x => x.CredentialId)
                .MatchesInstanceRule((x, y) => !(y.ConnectTypeId == FiscalRegistrarType.Software.Id && x is null), () => Resources.RequiredErrorMessage);
            builder.Property(x => x.IncombustibleAmount)
                .MatchesInstanceRule((x, y) => y.TypeId != CashboxType.FiscalRegistrar.Id || x.HasValue, () => Resources.RequiredErrorMessage);
        }

        object ICloneable.Clone()
        {
            return Clone();
        }

        public CashboxViewItem Clone()
        {
            return ReflectionObjectCloner.Clone(this);
        }

        private void TypeChanged()
        {
            if (TypeId == CashboxType.FiscalRegistrar.Id)
            {
                ConnectTypeId = CredentialId.HasValue ? FiscalRegistrarType.Software.Id : FiscalRegistrarType.Hardware.Id;
            }
            else
            {
                StrongboxId = null;
                CredentialId = null;
                ConnectTypeId = null;
            }

            if (TypeId != CashboxType.PaymentAccount.Id)
            {
                PaymentAccount = null;
            }

            if (TypeId != CashboxType.FiscalRegistrar.Id && TypeId != CashboxType.Employee.Id)
            {
                WarehouseId = null;
            }

            RaisePropertiesChanged(
                nameof(IncombustibleAmount),
                nameof(StrongboxId),
                nameof(AllowEditStrongbox),
                nameof(IsNotReadonlyPaymentAccount),
                nameof(AllowEditConnectType),
                nameof(AllowEditCredentials),
                nameof(WarehouseVisible));
        }

        private void ConnectTypeChanged()
        {
            if (ConnectTypeId == null || ConnectTypeId == FiscalRegistrarType.Hardware.Id)
            {
                CredentialId = null;
            }

            RaisePropertiesChanged(nameof(AllowEditCredentials), nameof(CredentialId));
        }

        private void CredentialChanged()
        {
            ConnectTypeId = CredentialId.HasValue ? FiscalRegistrarType.Software.Id : FiscalRegistrarType.Hardware.Id;
        }
    }
}