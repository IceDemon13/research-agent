using Telemart.Client.Data.Requests.Base.Action;

namespace Telemart.Client.Data.Requests.Features.Asterisk
{
    public sealed class SetAsteriskEmployeeStatus : CallEntityActionRequestResultBase<object>
    {
        public SetAsteriskEmployeeStatus(int statusId, int employeeId)
            : base(employeeId, $"{ApiResources.Asterisk}/employee_statuses", $"update/{statusId}")
        {
        }
    }
}