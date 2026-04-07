using DevExpress.Mvvm;

namespace Telemart.Client.ViewModels.ModuleAnalytics
{
    public class ModuleAnalyticsPositionParameter
    {
        public ModuleAnalyticsPositionParameter(string viewModelName, string viewModelTitle, ISupportServices parentViewModel)
        {
            ViewModelName = viewModelName;
            ViewModelTitle = viewModelTitle;
            ParentViewModel = parentViewModel;
        }

        public string ViewModelTitle { get; }

        public string ViewModelName { get; }

        public ISupportServices ParentViewModel { get; }
    }
}