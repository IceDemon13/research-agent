namespace Telemart.Client.ViewModels.Backlog
{
    public sealed class BacklogTaskAddToPlanParameter
    {
        public BacklogTaskAddToPlanParameter(BacklogTaskViewItem backlogTask)
        {
            BacklogTask = backlogTask;
        }

        public BacklogTaskViewItem BacklogTask { get; }
    }
}