using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Backlog;

namespace Telemart.Client.Data.Requests.Features.Backlog.Actions
{
    public sealed class SetBacklogTaskEstimate : UpdateEntityResultRequestBase<BacklogTaskDto, BacklogTaskEstimateDto>
    {
        public SetBacklogTaskEstimate(int id, int? estimate)
            : base(new BacklogTaskEstimateDto(id, estimate), ApiResources.BacklogTasks, id, "estimate")
        {
        }
    }
}