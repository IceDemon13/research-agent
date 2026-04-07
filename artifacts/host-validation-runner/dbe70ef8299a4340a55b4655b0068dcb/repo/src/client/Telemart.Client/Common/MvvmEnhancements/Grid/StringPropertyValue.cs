namespace Telemart.Client.Common.MvvmEnhancements.Grid
{
    public class StringPropertyValue : PropertyValueBase<string>
    {
        public StringPropertyValue(string value, int? entityId = null)
            : base(value)
        {
            EntityId = entityId;
        }

        public int? EntityId { get; }
    }
}