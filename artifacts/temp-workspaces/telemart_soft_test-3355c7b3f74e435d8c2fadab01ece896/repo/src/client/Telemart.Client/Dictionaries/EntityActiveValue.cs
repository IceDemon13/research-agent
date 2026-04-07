namespace Telemart.Client.Dictionaries
{
    public class EntityActiveValue : DictionaryItem
    {
        public EntityActiveValue(int id, string name)
            : base(id, name, true)
        {
        }

        public static EntityActiveValue ActiveValue { get; } = new EntityActiveValue(1, "Активен");

        public static EntityActiveValue NoActive { get; } = new EntityActiveValue(2, "Не активен");
    }
}
