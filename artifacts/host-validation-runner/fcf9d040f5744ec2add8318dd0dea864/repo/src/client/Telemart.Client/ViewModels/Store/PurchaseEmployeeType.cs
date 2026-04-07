using Telemart.Client.Dictionaries;

namespace Telemart.Client.ViewModels.Store
{
    public sealed class PurchaseEmployeeType : DictionaryItem
    {
        public PurchaseEmployeeType(int id, string name)
            : base(id, name, true)
        {
        }

        public static PurchaseEmployeeType SystemUser { get; } = new PurchaseEmployeeType(1, "Система");

        public static PurchaseEmployeeType CurrentUser { get; } = new PurchaseEmployeeType(2, "Я");

        public static PurchaseEmployeeType AnotherUser { get; } = new PurchaseEmployeeType(3, "Другой пользователь");
    }
}