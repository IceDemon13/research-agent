using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.Backlog;

namespace Telemart.Client.Data.Requests.Features.Backlog.Actions
{
    public class AddBacklogTaskToFavorites : CallEntityActionRequestResultBase<BacklogTaskDto>
    {
        public AddBacklogTaskToFavorites(int taskId)
            : base(taskId, ApiResources.BacklogTasks, "add_to_favorites")
        {
        }
    }
}