using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.Common.Messages
{
    public sealed class ServiceRepairViewMessage : EditorParameter
    {
        public ServiceRepairViewMessage(int id)
            : base(id)
        {
        }
    }
}