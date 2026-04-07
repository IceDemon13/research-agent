using DevExpress.XtraReports.UI;
using Telemart.Client.ReportDesigner;

namespace Telemart.Client.Reports.Order
{
    public partial class OrderTapeChequeReport : XtraReport
    {
        public OrderTapeChequeReport()
        {
            InitializeComponent();
        }

        private void OrderTapeChequeReport_BeforePrint(object sender, System.ComponentModel.CancelEventArgs e)
        {
            decimal deliveryCost = GetCurrentColumnValue<decimal>(nameof(OrderReportData.DeliveryCostDecimal));
            decimal prepaymentCostUah = GetCurrentColumnValue<decimal>(nameof(OrderReportData.PrepaymentCostUah));

            if (deliveryCost == 0)
            {
                xrLabel6.CanShrink = xrLabel7.CanShrink = true;
                xrLabel6.Text = xrLabel7.Text = string.Empty;
                xrLabel7.DataBindings.Clear();
            }

            if (prepaymentCostUah == 0)
            {
                xrLabel14.CanShrink = xrLabel15.CanShrink = true;
                xrLabel14.Text = xrLabel15.Text = string.Empty;
                xrLabel15.DataBindings.Clear();
            }
        }
    }
}
