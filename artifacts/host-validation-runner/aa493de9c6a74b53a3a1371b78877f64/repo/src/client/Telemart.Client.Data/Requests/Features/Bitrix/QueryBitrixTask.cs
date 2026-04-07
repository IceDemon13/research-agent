using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Bitrix;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.Data.Requests.Features.Bitrix
{
    public class QueryBitrixTask : QueryEntityRequestBase<Result<CategorizedBitrixTaskDto>>
    {
        public QueryBitrixTask(object id)
            : base(ApiResources.BitrixTasks, id)
        {
        }
    }
}