namespace Telemart.Client.ViewModels.Backlog
{
    public sealed class BacklogTaskJiraParameter
    {
        public BacklogTaskJiraParameter(BacklogTaskViewItem backlogTask)
        {
            BacklogTask = backlogTask;
        }

        public BacklogTaskViewItem BacklogTask { get; }
    }
}