using Telemart.Client.Business;

namespace Telemart.Client.ViewModels.Service.ServiceRequests
{
    public sealed class ServiceRequestCompensationPrice
    {
        private readonly string priceString;

        public ServiceRequestCompensationPrice(decimal value, int currencyId)
        {
            Value = value;
            CurrencyId = currencyId;

            priceString = CurrencyFormatingRules.ToStr(Value, CurrencyId);
        }

        public decimal Value { get; }

        public int CurrencyId { get; }

        public override string ToString()
        {
            return priceString;
        }
    }
}