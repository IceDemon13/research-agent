namespace Telemart.Client.ViewModels.TradeIn
{
    public sealed class CreateTradeInParameter
    {
        public CreateTradeInParameter(int tradeInCopy)
        {
            TradeInCopy = tradeInCopy;
        }

        public int TradeInCopy { get; }
    }
}