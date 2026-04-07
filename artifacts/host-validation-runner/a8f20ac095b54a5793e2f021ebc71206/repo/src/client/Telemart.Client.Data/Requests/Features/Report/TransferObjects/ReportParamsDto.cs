using System.Collections.Generic;

namespace Telemart.Client.Data.Requests.Features.Report.TransferObjects
{
    public class ReportParamsDto
    {
        public int Id { get; set; }

        public IReadOnlyCollection<ReportProcessedParameterDto> Parameters { get; set; }

        public IReadOnlyCollection<ReportFieldDto> Fields { get; set; }
    }
}