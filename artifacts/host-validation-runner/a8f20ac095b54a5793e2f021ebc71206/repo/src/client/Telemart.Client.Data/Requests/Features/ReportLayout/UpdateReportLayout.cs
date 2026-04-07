using Telemart.Client.Data.Requests.Base;
using Telemart.Client.Data.Requests.Features.ReportLayout.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.ReportLayout
{
    public sealed class UpdateReportLayout : UpdateEntityRequestBase<ReportLayoutDto, ReportLayoutSaveDto>
    {
        public UpdateReportLayout(int id, ReportLayoutSaveDto dto)
            : base(dto, "layouts", id)
        {
        }
    }
}