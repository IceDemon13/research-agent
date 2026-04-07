namespace Telemart.Client.Dictionaries
{
    public sealed class TagColor : DictionaryItem
    {
        public const int WhiteId = 1;
        public const int RedId = 2;
        public const int YellowId = 3;
        public const int GreenId = 4;

        public TagColor(int id, string name, string hex) : base(id, name, true)
        {
            Hex = hex;
        }
        public string Hex { get; }

        public static TagColor White { get; } = new TagColor(WhiteId, "Белый" , "FFFFFF");

        public static TagColor Red { get; } = new TagColor(RedId, "Красный" , "FF0000");

        public static TagColor Yellow { get; } = new TagColor(YellowId, "Желтый", "FFFF00");

        public static TagColor Green { get; } = new TagColor(GreenId, "Зеленый", "008000");
    }
}
