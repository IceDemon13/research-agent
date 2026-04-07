namespace Telemart.Client.Cache.Synchronization.Base
{
    public interface ISyncServiceBase
    {
        Task SyncAsync(CancellationToken cancellationToken);
    }
}