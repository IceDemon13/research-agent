namespace Telemart.Client.Dictionaries
{
    public sealed class TagFormat : DictionaryItem
    {
        private const int SmallId = 1;
        private const int NormalId = 2;
        private const int PromoId = 3;
        
        private TagFormat(int id, string name, string title)
            : base(id, name, true)
        {
            Title = title;
        }

        public static TagFormat Small { get; } = new TagFormat(SmallId, "small", "Маленький");

        public static TagFormat Normal { get; } = new TagFormat(NormalId, "normal", "Обычный");
        
        public static TagFormat Promo { get; } = new TagFormat(PromoId, "promo", "Акция");

        public string Title { get; }
    }
}