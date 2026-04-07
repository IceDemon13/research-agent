using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.CompanyStructure;

namespace Telemart.Client.Data.Requests.Features.CompanyStructure
{
    public sealed class QueryDepartments : QueryEntitiesRequestBase<DepartmentDto>
    {
        public QueryDepartments()
            : base(ApiResources.CompanyStructure, "departments")
        {
        }
    }
}