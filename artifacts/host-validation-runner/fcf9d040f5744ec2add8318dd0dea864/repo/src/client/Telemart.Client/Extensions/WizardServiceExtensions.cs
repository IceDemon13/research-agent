using System;
using DevExpress.Mvvm;

namespace Telemart.Client.Extensions
{
    public static class WizardServiceExtensions
    {
        public static void NavigateToView<T>(this IWizardService wizardService, object parameter, object parentViewModel)
            where T : ViewModelBase
        {
            NavigateToView(wizardService, typeof(T), parameter, parentViewModel);
        }

        public static void NavigateToView(this IWizardService wizardService, Type viewModelType, object parameter, object parentViewModel)
        {
            string viewName = viewModelType.Name.Replace("Model", string.Empty);
            wizardService.Navigate(viewName, null, parameter, parentViewModel);
        }
    }
}