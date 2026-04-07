namespace Telemart.Client.Reports
{
    public partial class TextReport : DevExpress.XtraReports.UI.XtraReport
    {
        public TextReport()
        {
            InitializeComponent();
        }

        private void TextReport_BeforePrint(object sender, System.ComponentModel.CancelEventArgs e)
        {
            int width = GetCurrentColumnValue<int>(nameof(TextReportData.Width));

            PageWidth = (int)(Margins.Left + Margins.Right + (width * 10));
        }
    }
}