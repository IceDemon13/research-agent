using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.Asterisk;

namespace Telemart.Client.Data.Requests.Features.Asterisk
{
    public sealed class UpdateAsteriskEmployees : CallActionWithBodyRequestResultBase<object, UpdateAsteriskEmployees.UpdateAsteriskEmployeesDto>
    {
        public UpdateAsteriskEmployees(AsteriskEmployeeSaveDto[] employeeMaxBusyTimes)
            : base(new UpdateAsteriskEmployeesDto(employeeMaxBusyTimes), ApiResources.Asterisk, "update_asterisk_employees")
        {
        }

        public class UpdateAsteriskEmployeesDto
        {
            public UpdateAsteriskEmployeesDto(AsteriskEmployeeSaveDto[] employeeMaxBusyTimes)
            {
                AsteriskEmployees = employeeMaxBusyTimes;
            }

            [JsonProperty("asterisk_employees")]
            public AsteriskEmployeeSaveDto[] AsteriskEmployees { get; }
        }
    }
}