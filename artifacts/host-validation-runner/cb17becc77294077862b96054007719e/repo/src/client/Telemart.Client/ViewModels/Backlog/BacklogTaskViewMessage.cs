using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Backlog
{
    public sealed class BacklogTaskViewMessage : EditorParameter
    {
        public BacklogTaskViewMessage(int taskId)
            : base(taskId)
        {
        }
    }
}
