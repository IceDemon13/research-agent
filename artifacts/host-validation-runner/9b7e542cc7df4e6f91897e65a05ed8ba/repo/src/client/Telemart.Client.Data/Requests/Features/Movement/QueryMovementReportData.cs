using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.MovementReport;

namespace Telemart.Client.Data.Requests.Features.Movement
{
    public class QueryMovementReportData : QueryEntityRequestBase<MovementReportDto>
    {
        public QueryMovementReportData(int id)
            : base(ApiResources.Movements, id, "report")
        {
        }
    }
}