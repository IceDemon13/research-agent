using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.AssemblyService;
using static Telemart.Client.Data.Requests.Features.AssemblyService.Actions.UpdateAssemblyServiceEmployee;

namespace Telemart.Client.Data.Requests.Features.AssemblyService.Actions
{
    public sealed class UpdateAssemblyServiceEmployee : CallEntityActionWithBodyRequestResultBase<AssemblyServiceDto, UpdateAssemblyServiceEmployeeDto>
    {
        public UpdateAssemblyServiceEmployee(int assemblyServiceId, UpdateAssemblyServiceEmployeeDto dto)
            : base(assemblyServiceId, dto, ApiResources.AssemblyService, "update_employee")
        {
        }

        public class UpdateAssemblyServiceEmployeeDto
        {
            public UpdateAssemblyServiceEmployeeDto(int id, int employeeId)
            {
                Id = id;
                EmployeeId = employeeId;
            }

            [JsonProperty("id")]
            public int Id { get; set; }

            [JsonProperty("employee_id")]
            public int EmployeeId { get; set; }
        }
    }
}