using Telemart.Client.ReportDesigner;

namespace Telemart.Client.Reports.Order
{
    public partial class OrderReportA5 : DevExpress.XtraReports.UI.XtraReport
    {
        public OrderReportA5()
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
