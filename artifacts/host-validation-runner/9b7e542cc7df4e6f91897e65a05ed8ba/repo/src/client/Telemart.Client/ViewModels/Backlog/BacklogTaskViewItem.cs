using System;
using System.ComponentModel;
using System.Linq;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Core.Cloning;
using Telemart.Client.Dictionaries;
using Telemart.Client.Properties;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Backlog
{
    public sealed class BacklogTaskViewItem : BindableBase, ILockableEntity, IDataErrorInfo, ICloneable
    {
        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public int? EmployeeId
        {
            get { return GetProperty(() => EmployeeId); }
            set { SetProperty(() => EmployeeId, value); }
        }

        public int AuthorId
        {
            get { return GetProperty(() => AuthorId); }
            set { SetProperty(() => AuthorId, value); }
        }

        public BacklogTaskState State
        {
            get { return GetProperty(() => State); }
            set { SetProperty(() => State, value); }
        }

        public BacklogTaskResolution Resolution
        {
            get { return GetProperty(() => Resolution); }
            set { SetProperty(() => Resolution, value); }
        }

        public Priority Priority
        {
            get { return GetProperty(() => Priority); }
            set { SetProperty(() => Priority, value); }
        }

        public int BitrixId
        {
            get { return GetProperty(() => BitrixId); }
            set { SetProperty(() => BitrixId, value); }
        }

        public int? QuotaId
        {
            get { return GetProperty(() => QuotaId); }
            set { SetProperty(() => QuotaId, value); }
        }

        public string JiraId
        {
            get { return GetProperty(() => JiraId); }
            set { SetProperty(() => JiraId, value); }
        }

        public int? Estimate
        {
            get { return GetProperty(() => Estimate); }
            set { SetProperty(() => Estimate, value); }
        }

        public string Name
        {
            get { return GetProperty(() => Name); }
            set { SetProperty(() => Name, value); }
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

        public string EmployeeLockName
        {
            get { return GetProperty(() => EmployeeLockName); }
            set { SetProperty(() => EmployeeLockName, value); }
        }

        public bool IsFavorite
        {
            get { return GetProperty(() => IsFavorite); }
            set { SetProperty(() => IsFavorite, value); }
        }

        public ComboBoxItem? Employee
        {
            get { return GetProperty(() => Employee); }
            set { SetProperty(() => Employee, value, () => EmployeeId = Employee?.Id); }
        }

        public int[] FavoriteEmployeeIds { get; set; }

        public int[] CategoryIds { get; set; }

        string IDataErrorInfo.Error => string.Empty;

        string IDataErrorInfo.this[string columnName] => IDataErrorInfoHelper.GetErrorText(this, columnName);

        public static void BuildMetadata(MetadataBuilder<BacklogTaskViewItem> builder)
        {
            builder.Property(x => x.Name)
                .Required(() => Resources.RequiredErrorMessage);

            builder.Property(x => x.State)
                .Required(() => Resources.RequiredErrorMessage);

            builder.Property(x => x.Priority)
                .Required(() => Resources.RequiredErrorMessage);

            builder.Property(x => x.JiraId)
                .MaxLength(10, () => "Длина поля должна быть не больше 10 символов");

            builder.Property(x => x.Employee)
                .MatchesInstanceRule(
                    (x, y) => x == null || x == default(ComboBoxItem) || y.State == BacklogTaskState.Completed || y.State == BacklogTaskState.Canceled || x.Value.Active,
                    () => "Поле не заполнено либо заполнено неактивным значением");

            builder.Property(x => x.CategoryIds)
                .MatchesInstanceRule(
                    (x, y) => x?.Any() == true,
                    () => Resources.RequiredErrorMessage);
        }

        object ICloneable.Clone()
        {
            return Clone();
        }

        public BacklogTaskViewItem Clone()
        {
            BacklogTaskViewItem item = ReflectionObjectCloner.Clone(this);

            item.CategoryIds = CategoryIds.OrderBy(x => x).ToArray();

            return item;
        }
    }
}