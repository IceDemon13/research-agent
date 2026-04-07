using Telemart.Client.Data.Requests.Base;
using Telemart.Client.Data.Requests.Features.Report.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Report
{
    public sealed class UpdateReport : UpdateEntityRequestBase<ReportDto, ReportSaveDto>
    {
        public UpdateReport(int id, ReportSaveDto dto)
            : base(dto, "reports", id)
        {
        }
    }
}