namespace Telemart.Client.Dictionaries
{
    public sealed class CategoryOverrideOption : DictionaryItem
    {
        private CategoryOverrideOption(int id, string name, bool active = true)
            : base(id, name, active)
        {
        }

        public static CategoryOverrideOption OverrideOnlyInCategory { get; } = new CategoryOverrideOption(1, "Только своё");

        public static CategoryOverrideOption OverrideInDescendantsWithSameValue { get; } = new CategoryOverrideOption(2, "Обычный");

        public static CategoryOverrideOption OverrideInAllDescendants { get; } = new CategoryOverrideOption(3, "Переназначить");
    }
}
