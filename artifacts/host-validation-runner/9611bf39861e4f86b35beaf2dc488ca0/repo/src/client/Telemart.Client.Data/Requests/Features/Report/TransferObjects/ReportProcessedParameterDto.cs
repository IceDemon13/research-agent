using System.Collections.Generic;

namespace Telemart.Client.Data.Requests.Features.Report.TransferObjects
{
    public class ReportProcessedParameterDto
    {
        public string Name { get; set; }

        public int EditorType { get; set; }

        public List<DataSourceItemDto> DataSource { get; set; }
    }
}