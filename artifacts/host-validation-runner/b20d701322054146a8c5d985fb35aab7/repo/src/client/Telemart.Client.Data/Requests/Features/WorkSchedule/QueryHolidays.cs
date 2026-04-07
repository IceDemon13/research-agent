using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.WorkSchedule;

namespace Telemart.Client.Data.Requests.Features.WorkSchedule
{
    public sealed class QueryHolidays : QueryEntitiesRequestBase<HolidayDto>
    {
        public QueryHolidays()
            : base($"{ApiResources.WorkSchedules}/{ApiResources.Holidays}")
        {
        }
    }
}