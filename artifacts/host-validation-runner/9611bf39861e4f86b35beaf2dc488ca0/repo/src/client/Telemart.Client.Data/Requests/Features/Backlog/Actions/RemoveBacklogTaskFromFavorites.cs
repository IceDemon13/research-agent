using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.Backlog;

namespace Telemart.Client.Data.Requests.Features.Backlog.Actions
{
    public class RemoveBacklogTaskFromFavorites : CallEntityActionRequestResultBase<BacklogTaskDto>
    {
        public RemoveBacklogTaskFromFavorites(int taskId)
            : base(taskId, ApiResources.BacklogTasks, "remove_from_favorites")
        {
        }
    }
}