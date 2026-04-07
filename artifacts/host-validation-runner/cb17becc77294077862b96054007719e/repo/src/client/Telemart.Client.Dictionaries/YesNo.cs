namespace Telemart.Client.Dictionaries
{
    public sealed class YesNo : DictionaryItemBase
    {
        public YesNo(int id, string name, bool boolean)
            : base(id, name)
        {
            Boolean = boolean;
        }

        public static YesNo Yes { get; } = new YesNo(1, "Да", true);

        public static YesNo No { get; } = new YesNo(2, "Нет", false);

        public bool Boolean { get; }
    }
}