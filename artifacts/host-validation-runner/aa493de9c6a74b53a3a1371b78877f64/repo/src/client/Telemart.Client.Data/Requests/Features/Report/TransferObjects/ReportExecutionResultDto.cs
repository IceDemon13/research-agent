using System.Collections.Generic;

namespace Telemart.Client.Data.Requests.Features.Report.TransferObjects
{
    public class ReportExecutionResultDto
    {
        public int Id { get; set; }

        public IReadOnlyCollection<object> Data { get; set; }

        public IReadOnlyCollection<ReportFieldDto> Fields { get; set; }
    }
}