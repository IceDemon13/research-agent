using System;
using System.Windows;
using System.Windows.Input;
using DevExpress.Xpf.Editors;
using DevExpress.Xpf.Grid;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.ViewModels.Store;

namespace Telemart.Client.Views.Store
{
    /// <summary>
    /// Interaction logic for StoreOrdersView.xaml
    /// </summary>
    public partial class StoreOrdersView
    {
        public StoreOrdersView()
        {
            InitializeComponent();
        }

        private void GridOnCustomColumnGroup(object sender, CustomColumnSortEventArgs e)
        {
            if (string.Equals(e.Column.FieldName, nameof(OrderViewItem.Carry), StringComparison.Ordinal))
            {
                CarryType value1 = (CarryType)e.Value1;
                CarryType value2 = (CarryType)e.Value2;

                e.Result = string.Compare(value1.NameShort, value2.NameShort, StringComparison.Ordinal);
                e.Handled = true;
            }
        }

        private void GridOnCustomColumnDisplayText(object sender, CustomColumnDisplayTextEventArgs e)
        {
            if (string.Equals(e.Column.FieldName, nameof(OrderViewItem.Carry), StringComparison.Ordinal))
            {
                CarryType carryType = (CarryType)e.Value;

                e.DisplayText = carryType.NameShort;
            }
        }

        private void TableView_CustomCellAppearance(object sender, CustomCellAppearanceEventArgs e)
        {
            e.Result = e.ConditionalValue;
            e.Handled = true;
        }
    }
}
