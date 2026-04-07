using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Bitrix;

namespace Telemart.Client.Data.Requests.Features.Bitrix
{
    public class QueryBitrixCategories : QueryEntitiesRequestBase<BitrixCategoryDto>
    {
        public QueryBitrixCategories()
            : base($"{ApiResources.BitrixTasks}/categories")
        {
        }
    }
}