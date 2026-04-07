using System.ComponentModel;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;

namespace Telemart.Client.ViewModels.Reporting.ViewItems
{
    public sealed class ReportFieldViewItem : BindableBase, IDataErrorInfo
    {
        public string UniqueName
        {
            get { return GetProperty(() => UniqueName); }
            set { SetProperty(() => UniqueName, value); }
        }

        public string Area
        {
            get { return GetProperty(() => Area); }
            set { SetProperty(() => Area, value); }
        }

        public string Caption
        {
            get { return GetProperty(() => Caption); }
            set { SetProperty(() => Caption, value); }
        }

        public string Description
        {
            get { return GetProperty(() => Description); }
            set { SetProperty(() => Description, value); }
        }

        public string CellFormat
        {
            get { return GetProperty(() => CellFormat); }
            set { SetProperty(() => CellFormat, value); }
        }

        public string FieldName
        {
            get { return GetProperty(() => FieldName); }
            set { SetProperty(() => FieldName, value); }
        }

        public string GroupInterval
        {
            get { return GetProperty(() => GroupInterval); }
            set { SetProperty(() => GroupInterval, value); }
        }

        public string SummaryType
        {
            get { return GetProperty(() => SummaryType); }
            set { SetProperty(() => SummaryType, value); }
        }

        public string DisplayFormat
        {
            get { return GetProperty(() => DisplayFormat); }
            set { SetProperty(() => DisplayFormat, value); }
        }

        public string EntityType
        {
            get { return GetProperty(() => EntityType); }
            set { SetProperty(() => EntityType, value); }
        }

        public string FilterPopupMode
        {
            get { return GetProperty(() => FilterPopupMode); }
            set { SetProperty(() => FilterPopupMode, value); }
        }

        #region IDataErrorInfo

        string IDataErrorInfo.Error => string.Empty;

        string IDataErrorInfo.this[string columnName] => IDataErrorInfoHelper.GetErrorText(this, columnName);

        #endregion

        public static void BuildMetadata(MetadataBuilder<ReportFieldViewItem> builder)
        {
            builder.Property(x => x.UniqueName)
                .MatchesRule(x => !string.IsNullOrWhiteSpace(x), () => Properties.Resources.RequiredErrorMessage);

            builder.Property(x => x.FieldName)
                .MatchesRule(x => !string.IsNullOrWhiteSpace(x), () => Properties.Resources.RequiredErrorMessage);

            builder.Property(x => x.Caption)
                .MatchesRule(x => !string.IsNullOrWhiteSpace(x), () => Properties.Resources.RequiredErrorMessage);

            builder.Property(x => x.Description)
                .MatchesRule(x => !string.IsNullOrWhiteSpace(x), () => Properties.Resources.RequiredErrorMessage);

            builder.Property(x => x.Area)
                .MatchesRule(x => !string.IsNullOrWhiteSpace(x), () => Properties.Resources.RequiredErrorMessage);

            builder.Property(x => x.Caption)
                .MatchesRule(x => !string.IsNullOrWhiteSpace(x), () => Properties.Resources.RequiredErrorMessage);

            builder.Property(x => x.SummaryType)
                .MatchesRule(x => !string.IsNullOrWhiteSpace(x), () => Properties.Resources.RequiredErrorMessage);
        }
    }
}