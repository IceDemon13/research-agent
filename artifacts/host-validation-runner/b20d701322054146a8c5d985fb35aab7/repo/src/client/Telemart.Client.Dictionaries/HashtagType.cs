namespace Telemart.Client.Dictionaries
{
    public class HashtagType : DictionaryItem
    {
        public const int PlusId = 1;
        public const int MinusId = 2;

        public HashtagType(int id, string name) : base(id, name, true)
        {
        }

        public static HashtagType Plus { get; } = new HashtagType(PlusId, "Плюс");

        public static HashtagType Minus { get; } = new HashtagType(MinusId, "Минус");
    }
}
