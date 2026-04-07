using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using DevExpress.Xpf.Bars;
using DevExpress.Xpf.Grid;
using DevExpress.Xpf.LayoutControl;
using Telemart.Client.Core.Helpers;

namespace Telemart.Client.Views.Store.Order.ProductInformation
{
    /// <summary>
    /// Interaction logic for ProductInformation.xaml.
    /// </summary>
    public partial class ProductInformation
    {
        public ProductInformation()
        {
            InitializeComponent();
        }

        private static GroupBox GetGroupBox(DependencyObject source)
        {
            DependencyObject obj = source;

            do
            {
                obj = VisualTreeHelper.GetParent(obj);
            }
            while (!(obj is GroupBox));

            GroupBox groupBox = (GroupBox)obj;

            return groupBox;
        }

        private static T FindChild<T>(DependencyObject parent, string childName = "")
            where T : DependencyObject
        {
            if (parent == null)
            {
                return null;
            }

            T foundChild = null;

            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);

            for (int i = 0; i < childrenCount; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);

                T childType = child as T;

                if (childType == null)
                {
                    // recursively drill down the tree
                    foundChild = FindChild<T>(child, childName);

                    // If the child is found, break so we do not overwrite the found child.
                    if (foundChild != null)
                    {
                        break;
                    }
                }
                else if (!string.IsNullOrEmpty(childName))
                {
                    if (child is FrameworkElement frameworkElement && frameworkElement.Name == childName)
                    {
                        foundChild = (T)child;
                        break;
                    }
                }
                else
                {
                    foundChild = (T)child;
                    break;
                }
            }

            return foundChild;
        }

        private void HyperlinkRequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            if (e.Uri.IsAbsoluteUri)
            {
                ProcessHelper.Start(e.Uri.AbsoluteUri);
            }

            e.Handled = true;
        }

        private void CopyingToClipboard(object sender, CopyingToClipboardEventArgs e)
        {
            if (sender is GridControl grid)
            {
                List<GridCell[]> gridCells = e.GridCells
                     .GroupBy(x => x.RowHandle)
                     .Select(g => g.OrderBy(y => y.Column.VisibleIndex).ToArray())
                     .ToList();

                // join every row cell with space and delimit rows with newline
                string text = gridCells.Aggregate(
                    new StringBuilder(),
                    (x, y) => x.AppendFormat("{0}{1}", string.Join(" ", y.Select(cell => GetGridCellValue(grid, cell))), Environment.NewLine)).ToString();

                Clipboard.SetText(text);

                e.Handled = true;
            }
        }

        private object GetGridCellValue(GridControl grid, GridCell cell)
        {
            if (grid == null || cell == null)
            {
                return null;
            }

            object value = grid.GetCellValue(cell.RowHandle, cell.Column);

            object result = value is DateTime dateTime
                ? dateTime.ToString("dd.MM")
                : value;

            return result;
        }

        private void GotGridFocus(object sender, RoutedEventArgs e)
        {
            if (sender is GridControl gridControl)
            {
                foreach (DependencyObject child in FlowLayoutControl.Children)
                {
                    if (child is GroupBox groupBox)
                    {
                        GridControl currentGridControl = FindChild<GridControl>(groupBox);

                        if (currentGridControl != null && !ReferenceEquals(gridControl, currentGridControl))
                        {
                            currentGridControl.SelectedItem = null;
                        }
                    }
                }
            }
        }

        private void ImagePreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ContextMenuManager.ShowElementContextMenu(sender);
        }

        private void ResizeButtonOnClick(object sender, RoutedEventArgs e)
        {
            GroupBox groupBox = GetGroupBox((DependencyObject)e.Source);

            if (groupBox.State == GroupBoxState.Normal)
            {
                groupBox.State = GroupBoxState.Minimized;
                groupBox.BorderThickness = new Thickness(0, 0, 0, 1);
            }
            else
            {
                groupBox.State = GroupBoxState.Normal;
                groupBox.BorderThickness = new Thickness(0, 0, 0, 0);
            }
        }
    }
}
