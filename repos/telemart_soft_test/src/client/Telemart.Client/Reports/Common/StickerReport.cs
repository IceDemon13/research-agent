using DevExpress.XtraReports.UI;
using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;

namespace Telemart.Client.Reports
{
    public partial class StickerReport : DevExpress.XtraReports.UI.XtraReport
    {
        public StickerReport()
        {
            InitializeComponent();
        }

        private void StickerReport_BeforePrint(object sender, CancelEventArgs e)
        {
            int width = GetCurrentColumnValue<int>(nameof(StickerReportData.Width));
            PageWidth = (int)(Margins.Left + Margins.Right + width);

            int height = GetCurrentColumnValue<int>(nameof(StickerReportData.Height));
            PageHeight = (int)(Margins.Top + Margins.Bottom + height);

            xrPictureBox1.SizeF = new SizeF(width, height);
        }
    }
}