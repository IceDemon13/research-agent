namespace Telemart.Client.Dictionaries
{
    public sealed class OrganizationPosition : DictionaryItem
    {
        private OrganizationPosition(int id, string name, bool active = true)
            : base(id, name, active)
        {
        }

        public static OrganizationPosition Director { get; } = new OrganizationPosition(1, "Директор");

        public static OrganizationPosition Accountant { get; } = new OrganizationPosition(2, "Бухгалтер");

        public static OrganizationPosition Manager { get; } = new OrganizationPosition(3, "Менеджер");

        public static OrganizationPosition Stockman { get; } = new OrganizationPosition(4, "Кладовщик");

        public static OrganizationPosition Driver { get; } = new OrganizationPosition(5, "Водитель");
    }
}
