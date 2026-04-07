namespace Telemart.Client.Dictionaries
{
    public class ServiceRequestAppearance : DictionaryItemBase
    {
        public const int LooksLikeNewId = 1;
        public const int LooksLikeUsedId = 2;
        public const int LooksLikePresaleId = 3;

        public ServiceRequestAppearance(int id, string name)
            : base(id, name)
        {
        }

        public static ServiceRequestAppearance LooksLikeNew { get; } = new ServiceRequestAppearance(LooksLikeNewId, "Як новий");

        public static ServiceRequestAppearance LooksLikeUsed { get; } = new ServiceRequestAppearance(LooksLikeUsedId, "Б/В");

        public static ServiceRequestAppearance LooksLikePresale { get; } = new ServiceRequestAppearance(LooksLikePresaleId, "Передпродажний");
    }
}
