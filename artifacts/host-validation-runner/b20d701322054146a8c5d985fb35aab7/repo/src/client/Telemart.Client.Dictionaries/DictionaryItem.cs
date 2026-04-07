namespace Telemart.Client.Dictionaries
{
    public abstract class DictionaryItem : DictionaryItemBase
    {
        protected DictionaryItem(int id, string name, bool active)
            : base(id, name)
        {
            Active = active;
        }

        public bool Active { get; }
    }
}