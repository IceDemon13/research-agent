using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.CompanyStructure;

namespace Telemart.Client.Data.Requests.Features.CompanyStructure
{
    public sealed class QueryDepartment : QueryEntityRequestBase<DepartmentDto>
    {
        public QueryDepartment(int id)
            : base(ApiResources.CompanyStructure, id, "department")
        {
        }
    }
}