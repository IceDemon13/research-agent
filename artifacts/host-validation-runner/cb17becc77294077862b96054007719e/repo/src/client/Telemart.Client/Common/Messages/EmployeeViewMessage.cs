using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.Common.Messages
{
    public sealed class EmployeeViewMessage : EditorParameter
    {
        public EmployeeViewMessage(int id)
            : base(id)
        {
        }
    }
}