using System.Collections.Generic;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.WorkSchedule;

namespace Telemart.Client.Data.Requests.Features.WorkSchedule
{
    public sealed class SaveHolidays : CallActionWithBodyRequestResultBase<List<HolidayDto>, SaveHolidaysDto>
    {
        public SaveHolidays(SaveHolidaysDto dto)
            : base(dto, $"{ApiResources.WorkSchedules}/{ApiResources.Holidays}", "save")
        {
        }
    }
}