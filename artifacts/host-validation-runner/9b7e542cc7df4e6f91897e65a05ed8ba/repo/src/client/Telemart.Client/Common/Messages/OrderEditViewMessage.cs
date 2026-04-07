using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.Common.Messages
{
    public class OrderEditViewMessage : EditorParameter
    {
        public OrderEditViewMessage(int orderId, bool editMode = false)
        : base(orderId)
        {
            EditMode = editMode;
        }

        public bool EditMode { get; }
    }
}