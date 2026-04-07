using DevExpress.Mvvm.UI;
using DevExpress.Xpf.Grid;

namespace Telemart.Client.Common.MvvmEnhancements
{
    internal sealed class RowDoubleClickEventArgsConverter : EventArgsConverterBase<RowDoubleClickEventArgs>
    {
        protected override object Convert(object sender, RowDoubleClickEventArgs args)
        {
            TableView view = (TableView)sender;

            return new RowDoubleClickInfo(args.HitInfo.Column.FieldName, view.Grid.CurrentItem);
        }
    }
}