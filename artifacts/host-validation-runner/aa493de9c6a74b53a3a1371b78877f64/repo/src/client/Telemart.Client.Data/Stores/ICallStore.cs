namespace Telemart.Client.Data.Stores
{
    public interface ICallStore
    {
        int? CallId { get; }

        void SetCallId(int callId);

        void Clear();
    }
}