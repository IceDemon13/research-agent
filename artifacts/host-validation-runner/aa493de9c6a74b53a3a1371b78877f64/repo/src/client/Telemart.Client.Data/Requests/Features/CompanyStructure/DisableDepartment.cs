using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.Data.Requests.Features.CompanyStructure
{
    public sealed class DisableDepartment : CallEntityActionRequestBase<Result>
    {
        public DisableDepartment(int id)
        : base(id, $"{ApiResources.CompanyStructure}/deparment", "disable")
        {
        }
    }
}