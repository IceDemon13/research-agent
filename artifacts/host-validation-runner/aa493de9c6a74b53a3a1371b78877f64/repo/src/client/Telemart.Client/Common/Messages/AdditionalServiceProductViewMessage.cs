using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.Common.Messages
{
    public sealed class AdditionalServiceProductViewMessage : EditorParameter
    {
        public AdditionalServiceProductViewMessage(int id)
        : base(id)
        {
        }
    }
}