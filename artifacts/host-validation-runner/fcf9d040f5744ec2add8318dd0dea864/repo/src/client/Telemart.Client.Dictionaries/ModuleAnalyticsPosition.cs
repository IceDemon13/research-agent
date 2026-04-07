namespace Telemart.Client.Dictionaries
{
    public sealed class ModuleAnalyticsPosition : DictionaryItem
    {
        private const int MaximizedId = 1;
        private const int MinimizedId = 2;
        private const int HiddenId = 3;

        private ModuleAnalyticsPosition(int id, string name)
            : base(id, name, true)
        {
        }

        public static ModuleAnalyticsPosition Maximized { get; } = new ModuleAnalyticsPosition(MaximizedId, "Окно открыто");

        public static ModuleAnalyticsPosition Minimized { get; } = new ModuleAnalyticsPosition(MinimizedId, "Окно свернуто");

        public static ModuleAnalyticsPosition Hidden { get; } = new ModuleAnalyticsPosition(HiddenId, "Окно скрыто");
    }
}