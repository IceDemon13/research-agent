using Telemart.Client.Data.Requests.Base;

namespace Telemart.Client.Data.Requests.Features.ReportLayout
{
    public sealed class DeleteReportLayout : DeleteEntityRequestBase
    {
        public DeleteReportLayout(int reportId)
            : base("layouts", reportId.ToString())
        {
        }
    }
}