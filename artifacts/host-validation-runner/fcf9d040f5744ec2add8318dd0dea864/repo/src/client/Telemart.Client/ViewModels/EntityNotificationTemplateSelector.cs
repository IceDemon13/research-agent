using System;
using System.Windows;
using System.Windows.Controls;

namespace Telemart.Client.ViewModels
{
    public class EntityNotificationTemplateSelector : DataTemplateSelector
    {
        private readonly Lazy<DataTemplate> dataTemplate;

        public EntityNotificationTemplateSelector()
        {
            dataTemplate = new Lazy<DataTemplate>(() =>
            {
                ResourceDictionary resourceDictionary = new ResourceDictionary
                {
                    Source = new Uri("/Telemart.Client;component/Style/MainStyle.xaml", UriKind.RelativeOrAbsolute)
                };

                return resourceDictionary["EntityNotificationTemplate"] as DataTemplate;
            });
        }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            return dataTemplate.Value;
        }
    }
}