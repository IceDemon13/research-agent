using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.Common.Messages
{
    public sealed class CallViewMessage : EditorParameter
    {
        public CallViewMessage(int callId)
            : base(callId)
        {
        }
    }
}
