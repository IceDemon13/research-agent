namespace Telemart.Client.Dictionaries
{
    public sealed class AbcType : DictionaryItem
    {
        public const int AId = 1;
        public const int BId = 2;
        public const int CId = 3;

        private AbcType(int id, string name)
            : base(id, name, true)
        {
        }

        public static AbcType A { get; } = new AbcType(AId, "A");

        public static AbcType B { get; } = new AbcType(BId, "B");

        public static AbcType C { get; } = new AbcType(CId, "C");
    }
}
