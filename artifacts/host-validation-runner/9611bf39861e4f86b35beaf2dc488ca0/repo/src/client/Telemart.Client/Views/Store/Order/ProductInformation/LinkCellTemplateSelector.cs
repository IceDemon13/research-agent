using System.Windows;
using System.Windows.Controls;
using DevExpress.Xpf.Grid;
using Telemart.Client.ViewModels.Store.Order.ProductInformation;

namespace Telemart.Client.Views.Store.Order.ProductInformation
{
    public class LinkCellTemplateSelector : DataTemplateSelector
    {
        public DataTemplate HyperlinkDataTemplate { get; set; }

        public DataTemplate TextDataTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is EditGridCellData cellData
                && cellData.RowData.Row is ILinkSupport link)
            {
                return string.IsNullOrWhiteSpace(link.Link)
                    ? TextDataTemplate
                    : HyperlinkDataTemplate;
            }

            return null;
        }
    }
}
