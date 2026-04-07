using System.Collections.Generic;
using DevExpress.Xpf.Grid;

namespace Telemart.Client.Views.Store.Order
{
    /// <summary>
    /// Interaction logic for MoveProductView.xaml
    /// </summary>
    public partial class MoveProductView
    {
        public MoveProductView()
        {
            InitializeComponent();
        }

        private void GridControl_OnCustomColumnSort(object sender, CustomColumnSortEventArgs e)
        {
            bool handled = false;
            switch (e.Column.FieldName)
            {
                case "Id":
                    if (e.Value2 == null)
                    {
                        e.Result = -1;
                        handled = true;
                    }

                    break;
                case "Fio":
                    if (e.Value2.ToString() == "Новый заказ")
                    {
                        e.Result = -1;
                        handled = true;
                    }

                    break;
                case "DeliveryTime":
                    if (e.Value2 == null)
                    {
                        e.Result = -1;
                        handled = true;
                    }

                    break;
            }

            if (handled == false)
            {
                e.Result = Comparer<object>.Default.Compare(e.Value1, e.Value2);
            }

            e.Handled = true;
        }
    }
}
