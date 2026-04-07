using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Telemart.Client.Core.Cloning;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Reporting.ViewItems
{
    public sealed class ReportViewItem : BindableBase, IDataErrorInfo, ICloneable, ILockableEntity
    {
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

        public string Description
        {
            get { return GetProperty(() => Description); }
            set { SetProperty(() => Description, value); }
        }

        public int? TimeoutSeconds
        {
            get { return GetProperty(() => TimeoutSeconds); }
            set { SetProperty(() => TimeoutSeconds, value); }
        }

        public string Query
        {
            get { return GetProperty(() => Query); }
            set { SetProperty(() => Query, value); }
        }

        public int? DatabaseId
        {
            get { return GetProperty(() => DatabaseId); }
            set { SetProperty(() => DatabaseId, value); }
        }

        public ObservableCollection<string> Roles
        {
            get { return GetProperty(() => Roles); }
            set { SetProperty(() => Roles, value); }
        }

        public ObservableCollection<int> Employees
        {
            get { return GetProperty(() => Employees); }
            set { SetProperty(() => Employees, value); }
        }

        public DateTime CreatedOn
        {
            get { return GetProperty(() => CreatedOn); }
            set { SetProperty(() => CreatedOn, value); }
        }

        public DateTime ModifiedOn
        {
            get { return GetProperty(() => ModifiedOn); }
            set { SetProperty(() => ModifiedOn, value); }
        }

        public int? ParentId
        {
            get { return GetProperty(() => ParentId); }
            set { SetProperty(() => ParentId, value); }
        }

        public bool IsFolder
        {
            get { return GetProperty(() => IsFolder); }
            set { SetProperty(() => IsFolder, value); }
        }

        public int? EmployeeLockId => null;

        public string EmployeeLockName => string.Empty;

        public ObservableCollection<ReportParameterViewItem> Parameters
        {
            get { return GetProperty(() => Parameters); }
            set { SetProperty(() => Parameters, value); }
        }

        public ObservableCollection<ReportFieldViewItem> Fields
        {
            get { return GetProperty(() => Fields); }
            set { SetProperty(() => Fields, value); }
        }

        #region IDataErrorInfo

        string IDataErrorInfo.Error => string.Empty;

        string IDataErrorInfo.this[string columnName] => IDataErrorInfoHelper.GetErrorText(this, columnName);

        #endregion

        public static void BuildMetadata(MetadataBuilder<ReportViewItem> builder)
        {
            builder.Property(x => x.Name)
                .MatchesRule(x => !string.IsNullOrWhiteSpace(x), () => Properties.Resources.RequiredErrorMessage);

            builder.Property(x => x.Description)
                .MatchesRule(x => !string.IsNullOrWhiteSpace(x), () => Properties.Resources.RequiredErrorMessage);

            builder.Property(x => x.Query)
                .MatchesRule(x => !string.IsNullOrWhiteSpace(x), () => Properties.Resources.RequiredErrorMessage);

            builder.Property(x => x.DatabaseId).Required(() => Properties.Resources.RequiredErrorMessage);
        }

        object ICloneable.Clone()
        {
            return Clone();
        }

        public ReportViewItem Clone()
        {
            ReportViewItem clonedItem = ReflectionObjectCloner.Clone(this);

            clonedItem.Parameters = Parameters.Select(x => ReflectionObjectCloner.Clone(x)).ToObservableCollection();
            clonedItem.Fields = Fields.Select(x => ReflectionObjectCloner.Clone(x)).ToObservableCollection();

            return clonedItem;
        }
    }
}