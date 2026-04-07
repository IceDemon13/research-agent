using System.Net;
using Telemart.Client.Data.Requests.Base;
using Telemart.Client.Data.Requests.Features.ReportLayout.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.ReportLayout
{
    // TODO: Change success status code to Created
    public sealed class CreateReportLayout : CreateEntityResultRequestBase<ReportLayoutDto, ReportLayoutSaveDto>
    {
        public CreateReportLayout(ReportLayoutSaveDto dto)
            : base(dto, "layouts")
        {
            SuccessStatusCode = HttpStatusCode.OK;
        }
    }
}