using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.Common.Messages
{
    public sealed class MovementViewMessage : EditorParameter
    {
        public MovementViewMessage(int movementId)
            : base(movementId)
        {
        }
    }
}