using System.Windows;
using System.Windows.Controls;
using DevExpress.Xpf.Grid;
using Telemart.Client.Dictionaries;

namespace Telemart.Client.ViewModels.RobotProperties
{
    public class RobotCellTemplateSelector : DataTemplateSelector
    {
        public DataTemplate TextDataTemplate { get; set; }

        public DataTemplate ComboBoxEditDataTemplate { get; set; }

        public DataTemplate MultiComboBoxEditDataTemplate { get; set; }

        public DataTemplate CheckBoxDataTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is EditGridCellData cellData && cellData.RowData.Row is IRobotTypeControl robotTypeControl)
            {
                return robotTypeControl.PropertyTypeId switch
                {
                    RobotPropertyType.FlagId => CheckBoxDataTemplate,
                    RobotPropertyType.ListId => ComboBoxEditDataTemplate,
                    RobotPropertyType.MultiListId => MultiComboBoxEditDataTemplate,
                    _ => TextDataTemplate
                };
            }

            return TextDataTemplate;
        }
    }
}