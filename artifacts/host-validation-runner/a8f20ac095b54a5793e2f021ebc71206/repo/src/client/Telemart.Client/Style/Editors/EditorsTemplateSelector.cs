using System.Windows;
using System.Windows.Controls;

namespace Telemart.Client.Style.Editors
{
    public class EditorsTemplateSelector : DataTemplateSelector
    {
        public DataTemplate ComboBox { get; set; }

        public DataTemplate MultiComboBox { get; set; }

        public DataTemplate Check { get; set; }

        public DataTemplate Default { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item == null)
            {
                return null;
            }

            return item switch
            {
                ComboBoxValue => ComboBox,
                MultiComboBoxValue => MultiComboBox,
                ValueWrapper<bool?> => Check,
                _ => Default
            };
        }
    }
}