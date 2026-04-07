using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Telemart.Client.Core.Cloning;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Directories.Employee
{
    public sealed class EmployeeRichViewItem : BindableBase, IDataErrorInfo, ICloneable, ILockableEntity
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

        public string EmployeeLockName
        {
            get { return GetProperty(() => EmployeeLockName); }
            set { SetProperty(() => EmployeeLockName, value); }
        }

        public int? CityId
        {
            get { return GetProperty(() => CityId); }
            set { SetProperty(() => CityId, value); }
        }

        public string Name
        {
            get { return GetProperty(() => Name); }
            set { SetProperty(() => Name, value); }
        }

        public string Login
        {
            get { return GetProperty(() => Login); }
            set { SetProperty(() => Login, value); }
        }

        public string Email
        {
            get { return GetProperty(() => Email); }
            set { SetProperty(() => Email, value); }
        }

        public string Phone1
        {
            get { return GetProperty(() => Phone1); }
            set { SetProperty(() => Phone1, value); }
        }

        public string Phone2
        {
            get { return GetProperty(() => Phone2); }
            set { SetProperty(() => Phone2, value); }
        }

        public string Skype
        {
            get { return GetProperty(() => Skype); }
            set { SetProperty(() => Skype, value); }
        }

        public string Telegram
        {
            get { return GetProperty(() => Telegram); }
            set { SetProperty(() => Telegram, value); }
        }

        public string Position
        {
            get { return GetProperty(() => Position); }
            set { SetProperty(() => Position, value); }
        }

        public int? SubdivisionId
        {
            get { return GetProperty(() => SubdivisionId); }
            set { SetProperty(() => SubdivisionId, value); }
        }

        public int DepartmentId
        {
            get { return GetProperty(() => DepartmentId); }
            set { SetProperty(() => DepartmentId, value); }
        }

        public int? ContractorTemplateId
        {
            get { return GetProperty(() => ContractorTemplateId); }
            set { SetProperty(() => ContractorTemplateId, value); }
        }

        public string CardKey
        {
            get { return GetProperty(() => CardKey); }
            set { SetProperty(() => CardKey, value); }
        }

        public bool ClientAccessDenied
        {
            get { return GetProperty(() => ClientAccessDenied); }
            set { SetProperty(() => ClientAccessDenied, value); }
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

        public DateTime? FiredOn
        {
            get { return GetProperty(() => FiredOn); }
            set { SetProperty(() => FiredOn, value); }
        }

        public int? FiredBy
        {
            get { return GetProperty(() => FiredBy); }
            set { SetProperty(() => FiredBy, value); }
        }

        public bool Active
        {
            get { return GetProperty(() => Active); }
            set { SetProperty(() => Active, value); }
        }

        public ObservableCollection<string> Roles
        {
            get { return GetProperty(() => Roles); }
            set { SetProperty(() => Roles, value); }
        }

        public ObservableCollection<int> AllowCashboxes
        {
            get { return GetProperty(() => AllowCashboxes); }
            set { SetProperty(() => AllowCashboxes, value); }
        }

        public ObservableCollection<int> AllowCategories
        {
            get { return GetProperty(() => AllowCategories); }
            set { SetProperty(() => AllowCategories, value); }
        }

        public ObservableCollection<int> AllowWarehouses
        {
            get { return GetProperty(() => AllowWarehouses); }
            set { SetProperty(() => AllowWarehouses, value); }
        }

        public ObservableCollection<int> AllowSubdivisions
        {
            get { return GetProperty(() => AllowSubdivisions); }
            set { SetProperty(() => AllowSubdivisions, value); }
        }

        public ObservableCollection<EmployeeAccountViewItem> Accounts
        {
            get { return GetProperty(() => Accounts); }
            set { SetProperty(() => Accounts, value); }
        }

        public ObservableCollection<EmployeeOperationViewItem> EmployeeOperations
        {
            get { return GetProperty(() => EmployeeOperations); }
            set { SetProperty(() => EmployeeOperations, value); }
        }

        #region IDataErrorInfo

        string IDataErrorInfo.Error => string.Empty;

        string IDataErrorInfo.this[string columnName] => IDataErrorInfoHelper.GetErrorText(this, columnName);

        #endregion

        public static void BuildMetadata(MetadataBuilder<EmployeeRichViewItem> builder)
        {
            builder.Property(x => x.CityId).Required();
            builder.Property(x => x.SubdivisionId).Required();
        }

        object ICloneable.Clone()
        {
            return Clone();
        }

        public EmployeeRichViewItem Clone()
        {
            EmployeeRichViewItem cloned = ReflectionObjectCloner.Clone(this);

            cloned.Accounts = Accounts.Select(x => ReflectionObjectCloner.Clone(x)).ToObservableCollection();
            cloned.EmployeeOperations = EmployeeOperations.Select(x => ReflectionObjectCloner.Clone(x)).ToObservableCollection();

            return cloned;
        }
    }
}