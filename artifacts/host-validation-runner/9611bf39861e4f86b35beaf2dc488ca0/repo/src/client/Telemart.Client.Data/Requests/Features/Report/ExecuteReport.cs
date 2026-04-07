using System.Collections.Generic;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.Data.Requests.Features.Report.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Report
{
    // TODO: Change dto to correct naming
    public sealed class ExecuteReport : CallEntityActionWithBodyRequestBase<List<object>, object>
    {
        public ExecuteReport(int reportId, int workPlaceId, IReadOnlyCollection<ReportParameterValueDto> parameterValues)
            : base(reportId, new { Id = reportId, WorkPlaceId = workPlaceId, ParameterValues = parameterValues }, "reports", "execute")
        {
        }
    }
}