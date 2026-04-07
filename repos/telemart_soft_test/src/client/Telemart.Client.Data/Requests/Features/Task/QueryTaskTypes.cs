using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Task;

namespace Telemart.Client.Data.Requests.Features.Task
{
    public sealed class QueryTaskTypes : QueryEntitiesRequestBase<TaskTypeDto>
    {
        public QueryTaskTypes()
            : base($"{ApiResources.Tasks}/types")
        {
        }
    }
}