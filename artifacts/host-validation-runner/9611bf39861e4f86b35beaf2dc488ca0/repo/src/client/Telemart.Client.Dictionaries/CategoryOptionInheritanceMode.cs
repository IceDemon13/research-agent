namespace Telemart.Client.Dictionaries
{
    public sealed class CategoryOptionInheritanceMode : DictionaryItem
    {
        private CategoryOptionInheritanceMode(int id, string name, bool active = true)
            : base(id, name, active)
        {
        }

        public static CategoryOptionInheritanceMode NonEditable { get; } = new CategoryOptionInheritanceMode(0, string.Empty);

        public static CategoryOptionInheritanceMode NonInheritable { get; } = new CategoryOptionInheritanceMode(1, string.Empty);

        public static CategoryOptionInheritanceMode InheritableToCategory { get; } = new CategoryOptionInheritanceMode(2, "Наследуется только категориями");

        public static CategoryOptionInheritanceMode InheritableToCategoryAndProduct { get; } = new CategoryOptionInheritanceMode(3, "Наследуется категориями и товарами");
    }
}
