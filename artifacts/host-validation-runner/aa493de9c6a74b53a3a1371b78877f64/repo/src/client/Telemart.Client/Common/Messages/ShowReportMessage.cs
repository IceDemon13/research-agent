namespace Telemart.Client.Common.Messages
{
    public sealed class ShowReportMessage
    {
        public ShowReportMessage(int reportId, string reportName)
        {
            ReportId = reportId;
            ReportName = reportName;
        }

        public int ReportId { get; }

        public string ReportName { get; }
    }
}
