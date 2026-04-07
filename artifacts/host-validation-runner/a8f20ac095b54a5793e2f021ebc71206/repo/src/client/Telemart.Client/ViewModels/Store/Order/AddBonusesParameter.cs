namespace Telemart.Client.ViewModels.Store.Order
{
    public sealed class AddBonusesParameter
    {
        public AddBonusesParameter(int orderId, int orderLeftToPay, int customerBonusesQuantity, string bonusTypeName)
        {
            OrderId = orderId;
            OrderLeftToPay = orderLeftToPay;
            CustomerBonusesQuantity = customerBonusesQuantity;
            BonusTypeName = bonusTypeName;
        }

        public int OrderId { get; }

        public int OrderLeftToPay { get; }

        public int CustomerBonusesQuantity { get; }

        public string BonusTypeName { get; }
    }
}