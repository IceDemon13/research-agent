namespace Telemart.Client.ViewModels.LogisticsMap
{
    public sealed class DiagramItemContext
    {
        public DiagramItemContext(int entityId, LogisticsMapEntity entity, string text)
        {
            EntityId = entityId;
            Entity = entity;
            Text = text;
        }

        public int EntityId { get; }

        public LogisticsMapEntity Entity { get; }

        public string Text { get; }
    }
}