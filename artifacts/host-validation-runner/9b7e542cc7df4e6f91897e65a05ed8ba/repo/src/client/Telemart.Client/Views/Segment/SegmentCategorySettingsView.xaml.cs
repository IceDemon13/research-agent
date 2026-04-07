using System;
using System.Windows.Controls;
using DevExpress.Xpf.Editors;

namespace Telemart.Client.Views.Segment
{
    public partial class SegmentCategorySettingsView
    {
        public SegmentCategorySettingsView()
        {
            InitializeComponent();
        }

        private void ComboBoxEdit_PopupOpening(object sender, OpenPopupEventArgs e)
        {
            e.Cancel = true;
        }
    }
}