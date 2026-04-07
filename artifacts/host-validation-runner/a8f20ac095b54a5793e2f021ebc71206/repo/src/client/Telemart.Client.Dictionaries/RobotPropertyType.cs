namespace Telemart.Client.Dictionaries
{
    public sealed class RobotPropertyType : DictionaryItemBase
    {
        public const int TextId = 1;
        public const int FlagId = 2;
        public const int ListId = 3;
        public const int MultiListId = 4;
        
        
        public RobotPropertyType(int id, string name)
            : base(id, name)
        {
        }

        public static RobotPropertyType Text { get; } = new RobotPropertyType(TextId, "Текст");

        public static RobotPropertyType Flag { get; } = new RobotPropertyType(FlagId, "Флаг");

        public static RobotPropertyType List { get; } = new RobotPropertyType(ListId, "Список");

        public static RobotPropertyType MultiList { get; } = new RobotPropertyType(MultiListId, "Список с мультивыбором");
    }
}