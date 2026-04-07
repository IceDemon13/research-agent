using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.WorkSchedule
{
    public sealed record WorkSchedulesDto
    {
        [JsonProperty("work_schedules")]
        public IReadOnlyCollection<SaveWorkScheduleDto> WorkSchedules { get; init; }
    }
}