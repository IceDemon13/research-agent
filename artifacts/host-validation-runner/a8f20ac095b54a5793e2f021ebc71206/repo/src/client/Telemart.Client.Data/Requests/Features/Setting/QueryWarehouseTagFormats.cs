using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Setting
{
    public sealed class QueryWarehouseTagFormats : QueryEntitiesPagedRequestBase<WarehouseTagFormatDto>
    {
        public QueryWarehouseTagFormats()
            : base($"{ApiResources.Settings}/tagformats")
        {
        }
    }
}
