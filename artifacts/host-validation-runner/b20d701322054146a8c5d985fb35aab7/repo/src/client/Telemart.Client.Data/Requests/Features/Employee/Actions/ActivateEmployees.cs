using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.Data.Requests.Features.Employee.Actions
{
    public sealed class ActivateEmployees : CallActionWithBodyRequestResultBase<object, ActivateEmployeesDto>
    {
        public ActivateEmployees(ActivateEmployeesDto dto)
            : base(dto, ApiResources.Employees, "activate")
        {
        }
    }
}