using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.WorkSchedule;

namespace Telemart.Client.Data.Requests.Features.WorkSchedule
{
    public sealed class QueryWorkSchedules : QueryEntitiesRequestBase<WorkScheduleDto>
    {
        public QueryWorkSchedules()
            : base(ApiResources.WorkSchedules, "work_schedule")
        {
        }
    }
}