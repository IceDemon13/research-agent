using Telemart.Client.ViewModels.Store.Call;

namespace Telemart.Client.Common.Messages
{
    public sealed class OutcomingCallViewMessage
    {
        public OutcomingCallViewMessage(CallViewItem call)
            : this(new[] { call })
        {
        }

        public OutcomingCallViewMessage(CallViewItem[] calls)
        {
            Calls = calls;
        }

        public CallViewItem[] Calls { get; }
    }
}
