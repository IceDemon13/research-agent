using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.WorkAccount;

namespace Telemart.Client.Data.Requests.Features.Employee.Actions
{
    public sealed class CreateTelewikiAccount : CallEntityActionWithBodyRequestResultBase<EmployeeRichDto, TelewikiCreateAccountDto>
    {
        public CreateTelewikiAccount(int id, string password)
            : base(id, new TelewikiCreateAccountDto(id, password), ApiResources.Employees, "create_wiki")
        {
        }
    }
}