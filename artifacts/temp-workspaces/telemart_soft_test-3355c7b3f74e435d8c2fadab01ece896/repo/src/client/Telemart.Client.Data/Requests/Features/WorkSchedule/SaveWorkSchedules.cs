using System.Collections.Generic;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.WorkSchedule;

namespace Telemart.Client.Data.Requests.Features.WorkSchedule
{
    public sealed class SaveWorkSchedules : CallActionWithBodyRequestResultBase<List<WorkScheduleDto>, WorkSchedulesDto>
    {
        public SaveWorkSchedules(WorkSchedulesDto dto)
            : base(dto, $"{ApiResources.WorkSchedules}/work_schedule", "save")
        {
        }
    }
}