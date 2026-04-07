using DevExpress.Mvvm.UI;
using DevExpress.Xpf.Grid;

namespace Telemart.Client.Common.MvvmEnhancements
{
    internal sealed class RowDoubleClickEventArgsTreeListConverter : EventArgsConverterBase<RowDoubleClickEventArgs>
    {
        protected override object Convert(object sender, RowDoubleClickEventArgs args)
        {
            TreeListView view = (TreeListView)sender;

            return new RowDoubleClickInfo(args.HitInfo.Column.FieldName, view.GetNodeByRowHandle(args.HitInfo.RowHandle).Content);
        }
    }
}
