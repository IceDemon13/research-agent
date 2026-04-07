namespace Telemart.Client.Dictionaries
{
    public class CategoryType : DictionaryItem
    {
        private const int PcComponentsId = 1;
        private const int PcPeripheralsId = 2;

        private CategoryType(int id, string name, int position)
            : base(id, name, true)
        {
            Posititon = position;
        }

        public int Posititon { get; }

        public static CategoryType PcComponents { get; } = new CategoryType(PcComponentsId, "Комплектующие ПК", 1);

        public static CategoryType PcPeripherals { get; } = new CategoryType(PcPeripheralsId, "Периферия ПК", 2);

        public static CategoryType Other { get; } = new CategoryType(0, "Прочее", 3);

        public override int CompareTo(DictionaryItemBase other)
        {
            return Posititon.CompareTo((other as CategoryType)?.Posititon);
        }
    }
}