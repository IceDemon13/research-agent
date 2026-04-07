namespace Telemart.Client.Dictionaries
{
    public class RepairDays : DictionaryItem
    {
        private const int Days14Value = 14;
        private const int Days30Value = 30;

        public RepairDays(int id, string name)
            : base(id, name, true)
        {
        }

        public static RepairDays Days14 { get; } = new RepairDays(Days14Value, "14 дней");

        public static RepairDays Days30 { get; } = new RepairDays(Days30Value, "30 дней");
    }
}
