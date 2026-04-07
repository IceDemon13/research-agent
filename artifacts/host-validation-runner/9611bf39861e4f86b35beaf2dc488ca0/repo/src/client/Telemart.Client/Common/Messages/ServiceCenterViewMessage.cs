using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.Common.Messages
{
    public sealed class ServiceCenterViewMessage : EditorParameter
    {
        public ServiceCenterViewMessage(int id)
            : base(id)
        {
        }
    }
}