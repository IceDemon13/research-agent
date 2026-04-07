using System;
using System.ComponentModel;
using System.Windows.Input;
using DevExpress.Data;
using DevExpress.Xpf.Editors;
using DevExpress.Xpf.Grid;

namespace Telemart.Client.Views.Directories.ProductsCatalog
{
    /// <summary>
    /// Interaction logic for ProductsCatalogView.xaml.
    /// </summary>
    public partial class ProductsCatalogView
    {
        public ProductsCatalogView()
        {
            InitializeComponent();
        }

        private void GridControlPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!GridControl.View.AllowSorting)
            {
                foreach (GridColumn gridColumn in TableView.Grid.Columns)
                {
                    gridColumn.SortOrder = ColumnSortOrder.None;
                }
            }
        }

        private void GridControl_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            GridControl gridControl = sender as GridControl;
            DataViewBase view = gridControl.View;

            if (Keyboard.Modifiers == ModifierKeys.None
                && view.ActiveEditor != null
                && ((e.Key == Key.Left && (view.ActiveEditor as TextEdit).CaretIndex == 0)
                 || (e.Key == Key.Right && (view.ActiveEditor as TextEdit).CaretIndex == (view.ActiveEditor as TextEdit).Text.Length)
                 || (e.Key == Key.Up)
                 || (e.Key == Key.Down)))
            {
                e.Handled = true;
            }
        }

        private void TableView_ShownEditor(object sender, EditorEventArgs e)
        {
            TextEdit editor = e.Editor as TextEdit;
            Dispatcher.BeginInvoke(new Action(() => { editor.Select(editor.Text.Length, 0); }));
        }
    }
}
