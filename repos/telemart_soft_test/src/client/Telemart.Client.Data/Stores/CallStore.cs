namespace Telemart.Client.Data.Stores
{
    public sealed class CallStore : ICallStore
    {
        private int? callId;

        public int? CallId => callId;

        public void SetCallId(int callId)
        {
            this.callId = callId;
        }

        public void Clear()
        {
            callId = null;
        }
    }
}