using Telemart.Client.ReportDesigner;

namespace Telemart.Client.Reports.Order
{
    public partial class OrderReport : DevExpress.XtraReports.UI.XtraReport
    {
        public OrderReport()
        {
            InitializeComponent();
        }

        private void ReportFooter1BeforePrint(object sender, System.ComponentModel.CancelEventArgs e)
        {
            decimal prepaymentCostUsd = GetCurrentColumnValue<decimal>(nameof(OrderReportData.PrepaymentCostUsd));
            decimal prepaymentCostUah = GetCurrentColumnValue<decimal>(nameof(OrderReportData.PrepaymentCostUah));

            xrTableRow2.Visible = prepaymentCostUah != 0 || prepaymentCostUsd != 0;
        }
    }
}
