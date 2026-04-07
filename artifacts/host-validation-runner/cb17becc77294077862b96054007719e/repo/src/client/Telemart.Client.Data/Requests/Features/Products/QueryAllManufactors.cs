using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Products
{
    public sealed class QueryAllManufactors : QueryRequestBase<ManufactorsDto>
    {
        public QueryAllManufactors()
            : base(ApiResources.Products, "manufactors")
        {
        }
    }
}