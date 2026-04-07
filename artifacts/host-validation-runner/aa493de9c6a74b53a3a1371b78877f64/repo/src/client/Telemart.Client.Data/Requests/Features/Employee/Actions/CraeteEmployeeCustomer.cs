using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Employee.Actions
{
    public sealed class CraeteEmployeeCustomer : CallEntityActionWithBodyRequestResultBase<EmployeeRichDto, EmployeeCustomerCreateDto>
    {
        public CraeteEmployeeCustomer(EmployeeCustomerCreateDto createDto)
            : base(createDto.EmployeeId, createDto, ApiResources.Employees, "customer")
        {
        }
    }
}