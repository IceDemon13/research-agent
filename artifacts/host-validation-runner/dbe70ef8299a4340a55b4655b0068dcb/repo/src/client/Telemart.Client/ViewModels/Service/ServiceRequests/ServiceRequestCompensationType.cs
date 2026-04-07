using Telemart.Client.Dictionaries;

namespace Telemart.Client.ViewModels.Service.ServiceRequests
{
    public sealed class ServiceRequestCompensationType : DictionaryItem
    {
        private const int CompensateOnBalanceId = 1;
        private const int CompensateByProductId = 2;
        private const int CompensateReturnMoneyId = 3;

        public ServiceRequestCompensationType(int id, string name)
            : base(id, name, true)
        {
        }

        public static ServiceRequestCompensationType CompensateOnBalance { get; } = new ServiceRequestCompensationType(CompensateOnBalanceId, "Зачесть на баланс");

        public static ServiceRequestCompensationType CompensateByProduct { get; } = new ServiceRequestCompensationType(CompensateByProductId, "Создать обменный заказ");

        public static ServiceRequestCompensationType CompensateReturnMoney { get; } = new ServiceRequestCompensationType(CompensateReturnMoneyId, "Возврат ДС");
    }
}