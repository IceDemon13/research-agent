namespace Telemart.Client.Dictionaries
{
    public sealed class CarryTypeKind : DictionaryItem
    {
        public const int PickupId = 1;
        public const int CourierId = 2;

        private CarryTypeKind(int id, string name)
            : base(id, name, true)
        {
        }

        public static CarryTypeKind Pickup { get; } = new CarryTypeKind(PickupId, "Самовывоз");

        public static CarryTypeKind Courier { get; } = new CarryTypeKind(CourierId, "Курьер");
    }
}