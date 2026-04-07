using System.Net;
using Telemart.Client.Data.Requests.Base;
using Telemart.Client.Data.Requests.Features.Report.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Report
{
    public sealed class CreateReport : CreateEntityRequestBase<ReportDto, ReportSaveDto>
    {
        public CreateReport(ReportSaveDto dto)
            : base(dto, "reports")
        {
            SuccessStatusCode = HttpStatusCode.OK;
        }
    }
}