using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;
using static Telemart.Client.Data.Requests.Features.AdditionalServiceProduct.Actions.UpdateAdditionalServiceProductEmployee;

namespace Telemart.Client.Data.Requests.Features.AdditionalServiceProduct.Actions
{
    public sealed class UpdateAdditionalServiceProductEmployee : CallEntityActionWithBodyRequestResultBase<AdditionalServiceProductDto, UpdateAdditionalServiceProductEmployeeDto>
    {
        public UpdateAdditionalServiceProductEmployee(int additionalServiceProductId, UpdateAdditionalServiceProductEmployeeDto dto)
            : base(additionalServiceProductId, dto, ApiResources.AdditionalServicesProducts, "update_employee")
        {
        }

        public class UpdateAdditionalServiceProductEmployeeDto
        {
            public UpdateAdditionalServiceProductEmployeeDto(int id, int employeeId)
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