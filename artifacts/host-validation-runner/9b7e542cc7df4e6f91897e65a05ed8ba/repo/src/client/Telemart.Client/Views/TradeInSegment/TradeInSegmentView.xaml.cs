using System.Windows.Controls;
using DevExpress.Xpf.Editors;

namespace Telemart.Client.Views.TradeInSegment
{
    public partial class TradeInSegmentView : UserControl
    {
        public TradeInSegmentView()
        {
            InitializeComponent();
        }

        private void ComboBoxEdit_PopupOpening(object sender, OpenPopupEventArgs e)
        {
            e.Cancel = true;
        }
    }
}