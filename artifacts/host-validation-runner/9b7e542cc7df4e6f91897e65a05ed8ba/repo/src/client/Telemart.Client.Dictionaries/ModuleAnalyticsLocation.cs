namespace Telemart.Client.Dictionaries
{
    public sealed class ModuleAnalyticsLocation : DictionaryItem
    {
        private const int HorizontalId = 1;
        private const int CustomId = 2;

        private ModuleAnalyticsLocation(int id, string name)
            : base(id, name, true)
        {
        }

        public static ModuleAnalyticsLocation Horizontal { get; } = new ModuleAnalyticsLocation(HorizontalId, "Горизонтальное");

        public static ModuleAnalyticsLocation Custom { get; } = new ModuleAnalyticsLocation(CustomId, "Ручное");
    }
}