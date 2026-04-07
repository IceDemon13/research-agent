using System.Collections.Generic;
using System.Windows.Input;
using DevExpress.Data;
using DevExpress.Xpf.Bars;
using DevExpress.Xpf.Grid;
using Telemart.Client.ViewModels.Directories.ProductsPrices;

namespace Telemart.Client.Views.Directories.ProductsPrices
{
    /// <summary>
    /// Interaction logic for ProductPricesView.xaml.
    /// </summary>
    public partial class ProductPricesView
    {
        public ProductPricesView()
        {
            InitializeComponent();
        }

        private void TableViewOnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (GridControl.CurrentColumn != null && GridControl.CurrentColumn.FieldName.EndsWith("DisplayPrice"))
            {
                GridControl.View.CloseEditor();
                e.Handled = true;
            }
        }

        private void TableViewOnHiddenEditor(object sender, EditorEventArgs e)
        {
            if (GridControl.CurrentItem != null)
            {
                ProductPriceViewItem item = (ProductPriceViewItem)GridControl.CurrentItem;

                item.RaisePricePropertiesChanged();
            }
        }

        private void BarItemClearUpItemClick(object sender, ItemClickEventArgs e)
        {
            if (GridControl.ItemsSource is ICollection<ProductPriceViewItem> && GridControl.CurrentItem != null)
            {
                for (int rowHandle = GridControl.View.FocusedRowHandle - 1; rowHandle >= 0; rowHandle--)
                {
                    ProductPriceViewItem item = (ProductPriceViewItem)GridControl.GetRow(rowHandle);
                    item.ResetPriceAndAvailChanges();
                }

                e.Handled = true;
            }
        }

        private void BarItemClearDownItemClick(object sender, ItemClickEventArgs e)
        {
            if (GridControl.ItemsSource is ICollection<ProductPriceViewItem> items && GridControl.CurrentItem != null)
            {
                for (int rowHandle = GridControl.View.FocusedRowHandle + 1; rowHandle < items.Count; rowHandle++)
                {
                    ProductPriceViewItem item = (ProductPriceViewItem)GridControl.GetRow(rowHandle);
                    item.ResetPriceAndAvailChanges();
                }

                e.Handled = true;
            }
        }

        private void TableViewOnCustomCellAppearance(object sender, CustomCellAppearanceEventArgs e)
        {
            e.Result = e.ConditionalValue;
            e.Handled = true;
        }

        private void GridControlOnCustomColumnSort(object sender, CustomColumnSortEventArgs e)
        {
            if (e.Column.FieldName == nameof(ProductPriceViewItem.HotlinePosition))
            {
                if (e.Value1 != null && e.Value2 == null)
                {
                    e.Result = e.SortOrder == ColumnSortOrder.Ascending ? -1 : 1;
                    e.Handled = true;
                }

                if (e.Value1 == null && e.Value2 != null)
                {
                    e.Result = e.SortOrder == ColumnSortOrder.Ascending ? 1 : -1;
                    e.Handled = true;
                }
            }
        }
    }
}
