using System.Drawing.Printing;
using DevExpress.XtraReports.UI;
using Telemart.Client.Dictionaries;

namespace Telemart.Client.ReportDesigner
{
    public partial class MovementReport : XtraReport
    {
        private bool firstPage = true;

        public MovementReport()
        {
            InitializeComponent();
        }

        private void MovementReport_BeforePrint(object sender, System.ComponentModel.CancelEventArgs e)
        {
            int stateId = GetCurrentColumnValue<int>(nameof(MovementReportData.StateId));

            if (stateId != MovementState.New.Id)
            {
                xrTable2.DeleteColumn(xrTableCell10);
                xrTable4.DeleteColumn(xrTableCell18);
            }
        }

        private void PageHeader_BeforePrint(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (firstPage)
            {
                e.Cancel = true;
                firstPage = false;
            }
        }
    }
}