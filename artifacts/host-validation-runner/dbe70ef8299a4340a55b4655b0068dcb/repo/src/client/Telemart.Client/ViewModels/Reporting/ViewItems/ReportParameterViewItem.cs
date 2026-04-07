using System.ComponentModel;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.ViewModels.Reporting.Mvvm;

namespace Telemart.Client.ViewModels.Reporting.ViewItems
{
    public sealed class ReportParameterViewItem : BindableBase, IDataErrorInfo
    {
        public string Name
        {
            get { return GetProperty(() => Name); }
            set { SetProperty(() => Name, value); }
        }

        public string SqlName
        {
            get { return GetProperty(() => SqlName); }
            set { SetProperty(() => SqlName, value); }
        }

        public ReportParameterEditorType EditorType
        {
            get { return GetProperty(() => EditorType); }
            set { SetProperty(() => EditorType, value, () => { RaisePropertyChanged(nameof(DataSourceId)); }); }
        }

        public string DataSource
        {
            get { return GetProperty(() => DataSource); }
            set { SetProperty(() => DataSource, value); }
        }

        public bool DataSourceIsSecured
        {
            get { return GetProperty(() => DataSourceIsSecured); }
            set { SetProperty(() => DataSourceIsSecured, value); }
        }

        public int? DataSourceId
        {
            get { return GetProperty(() => DataSourceId); }
            set { SetProperty(() => DataSourceId, value); }
        }

        #region IDataErrorInfo

        string IDataErrorInfo.Error => string.Empty;

        string IDataErrorInfo.this[string columnName] => IDataErrorInfoHelper.GetErrorText(this, columnName);

        #endregion

        public static void BuildMetadata(MetadataBuilder<ReportParameterViewItem> builder)
        {
            builder.Property(x => x.Name)
                .MatchesRule(x => !string.IsNullOrWhiteSpace(x), () => Properties.Resources.RequiredErrorMessage);

            builder.Property(x => x.SqlName)
                .MatchesRule(x => !string.IsNullOrWhiteSpace(x), () => Properties.Resources.RequiredErrorMessage);
        }
    }
}