using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Backlog;

namespace Telemart.Client.Data.Requests.Features.Backlog
{
    public class QueryBacklogCategories : QueryEntitiesRequestBase<BacklogCategoryDto>
    {
        public QueryBacklogCategories()
            : base($"{ApiResources.BacklogTasks}/categories")
        {
        }
    }
}