namespace Telemart.Client.Common.Messages
{
    public class ModuleAnalyticsCloseMessage
    {
        public ModuleAnalyticsCloseMessage(int entityId)
        {
            EntityId = entityId;
        }

        public int EntityId { get; }
    }
}