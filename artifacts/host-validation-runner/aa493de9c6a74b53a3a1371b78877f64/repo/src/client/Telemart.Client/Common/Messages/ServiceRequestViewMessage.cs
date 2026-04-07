using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.Common.Messages
{
    public sealed class ServiceRequestViewMessage : EditorParameter
    {
        public ServiceRequestViewMessage(int id)
            : base(id)
        {
        }
    }
}