namespace Telemart.Client.Dictionaries
{
    public class ExpireReasonType : DictionaryItem
    {
        public ExpireReasonType(int id, string name, int entityId)
            : base(id, name, true)
        {
            EntityId = entityId;
        }

        public int EntityId { get; }
    }
}