using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.WorkSchedule;

namespace Telemart.Client.Data.Requests.Features.WorkSchedule
{
    public sealed class QueryWorkScheduleTypes : QueryEntitiesRequestBase<WorkScheduleTypeDto>
    {
        public QueryWorkScheduleTypes()
            : base(ApiResources.WorkSchedules, "schedule_types")
        {
        }
    }
}