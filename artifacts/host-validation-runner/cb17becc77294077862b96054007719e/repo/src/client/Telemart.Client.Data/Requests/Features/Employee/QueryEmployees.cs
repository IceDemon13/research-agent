using System;
using Telemart.Client.Data.Requests.Base;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Employee
{
    public sealed class QueryEmployees : QueryEntitiesPagedRequestBase<EmployeeDto>
    {
        public QueryEmployees()
            : base(ApiResources.Employees)
        {
        }

        public QueryEmployees(DateTime? modifiedOnAfter)
            : base(new ModifiedOnAfterFilteringItem { ModifiedOnAfter = modifiedOnAfter }, ApiResources.Employees)
        {
        }

        public QueryEmployees(int? accountId, int? departmentId, bool? active = null, int[]? positionIds = null)
            : base(new EmployeesFilter(accountId, departmentId, active, positionIds), ApiResources.Employees)
        {
        }
    }

    public class EmployeesFilter : FilteringItemBase
    {
        public EmployeesFilter(int? accountId, int? departmentId, bool? active, int[]? positionIds)
        {
            AccountId = accountId;
            DepartmentId = departmentId;
            Active = active;
            PositionIds = positionIds;
        }

        [FilteringItemProperty("account_id")]
        public int? AccountId { get; }

        [FilteringItemProperty("department_id")]
        public int? DepartmentId { get; }

        [FilteringItemProperty("active")]
        public bool? Active { get; init; }

        [FilteringItemProperty("position_ids")]
        public int[]? PositionIds { get; init; }
    }
}