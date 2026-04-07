using System;
using System.Windows;
using DevExpress.Xpf.Grid;
using Telemart.Client.ViewModels.Warehouse.Movement;

namespace Telemart.Client.Views.Warehouse.Movement
{
    public partial class MovementsMassScanView
    {
        public MovementsMassScanView()
        {
            InitializeComponent();
        }

        private void GridControlOnCustomColumnSort(object sender, CustomColumnSortEventArgs e)
        {
            if (string.Equals(e.Column.FieldName, nameof(MovementProductViewItem.ProductParentCategoryName), StringComparison.Ordinal))
            {
                MovementProductViewItem m1 = (MovementProductViewItem)GridControl.GetRow(e.ListSourceRowIndex1);
                MovementProductViewItem m2 = (MovementProductViewItem)GridControl.GetRow(e.ListSourceRowIndex2);

                e.Result = m1.ProductParentCategoryLeft.CompareTo(m2.ProductParentCategoryLeft);

                e.Handled = true;
            }
        }

        private void ButtonInfoOnClick(object sender, RoutedEventArgs e)
        {
            GridControl.View.HideEditor();
        }
    }
}