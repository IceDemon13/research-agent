namespace Telemart.Client.Dictionaries
{
    public sealed class PriceRobotMode : DictionaryItem
    {
        public const int OnlyParametersId = 1;
        public const int AvailMinusId = 2;
        public const int AvailPlusId = 3;
        public const int PriceId = 4;
        public const int NoId = 5;

        private PriceRobotMode(int id, string name, string nameShort)
            : base(id, name, true)
        {
            NameShort = nameShort;
        }

        public static PriceRobotMode OnlyParameters { get; } = new PriceRobotMode(OnlyParametersId, "Только параметры", "ТП");

        public static PriceRobotMode AvailMinus { get; } = new PriceRobotMode(AvailMinusId, "Наличие-", "Н-");

        public static PriceRobotMode AvailPlus { get; } = new PriceRobotMode(AvailPlusId, "Наличие+", "Н+");

        public static PriceRobotMode Price { get; } = new PriceRobotMode(PriceId, "Цена", "+");

        public static PriceRobotMode No { get; } = new PriceRobotMode(NoId, "Выключен", "-");

        public string NameShort { get; }

        public static bool operator <(PriceRobotMode x, PriceRobotMode y)
        {
            return x.Id < y.Id;
        }

        public static bool operator >(PriceRobotMode x, PriceRobotMode y)
        {
            return x.Id > y.Id;
        }

        public static bool operator <=(PriceRobotMode x, PriceRobotMode y)
        {
            return x.Id <= y.Id;
        }

        public static bool operator >=(PriceRobotMode x, PriceRobotMode y)
        {
            return x.Id >= y.Id;
        }

        public int CompareTo(PriceRobotMode other)
        {
            return Id.CompareTo(other.Id);
        }

        public override string ToString()
        {
            return NameShort;
        }
    }
}