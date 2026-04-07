namespace Telemart.Client.Dictionaries
{
    public class BitrixTaskRole : DictionaryItem
    {
        public const int ResponsibleId = 1;
        public const int AccompliceId = 2;
        public const int AuditorId = 3;

        private BitrixTaskRole(int id, string name)
            : base(id, name, true)
        {
        }

        public static BitrixTaskRole Responsible { get; } = new BitrixTaskRole(ResponsibleId, "Ответственный");

        public static BitrixTaskRole Accomplice { get; } = new BitrixTaskRole(AccompliceId, "Соисполнитель");

        public static BitrixTaskRole Auditor { get; } = new BitrixTaskRole(AuditorId, "Наблюдатель");

    }
}
