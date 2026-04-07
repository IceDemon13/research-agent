using DevExpress.Mvvm;
using DevExpress.Xpf.PivotGrid;

namespace Telemart.Client.Common.Behaviors
{
    public sealed class PivotGridFieldItem : BindableBase
    {
        public FieldArea Area
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

        public string UniqueName
        {
            get { return GetProperty(() => UniqueName); }
            set { SetProperty(() => UniqueName, value); }
        }

        public FieldGroupInterval GroupInterval
        {
            get { return GetProperty(() => GroupInterval); }
            set { SetProperty(() => GroupInterval, value); }
        }

        public FieldSummaryType SummaryType
        {
            get { return GetProperty(() => SummaryType); }
            set { SetProperty(() => SummaryType, value); }
        }
    }
}