using DevExpress.Mvvm;

namespace Telemart.Client.ViewModels.Reporting.ViewItems
{
    public class ReportLegendViewItem : BindableBase
    {
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
    }
}
