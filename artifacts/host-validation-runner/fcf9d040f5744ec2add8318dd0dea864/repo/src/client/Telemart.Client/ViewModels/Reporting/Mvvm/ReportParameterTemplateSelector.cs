using System;
using System.Windows;
using System.Windows.Controls;

namespace Telemart.Client.ViewModels.Reporting.Mvvm
{
    public sealed class ReportParameterTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            ReportParameter reportParameter = (ReportParameter)item;
            ContentPresenter control = (ContentPresenter)container;

            string templateName = $"{reportParameter.EditorType:G}DataTemplate";

            return (DataTemplate)control.FindResource(templateName);
        }
    }
}