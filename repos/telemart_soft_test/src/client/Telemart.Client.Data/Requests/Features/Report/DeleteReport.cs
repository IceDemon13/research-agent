using Telemart.Client.Data.Requests.Base;

namespace Telemart.Client.Data.Requests.Features.Report
{
    public sealed class DeleteReport : DeleteEntityRequestBase
    {
        public DeleteReport(int reportId)
            : base("reports", reportId.ToString())
        {
        }
    }
}