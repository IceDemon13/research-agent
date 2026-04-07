namespace Telemart.Client.ViewModels.TradeIn
{
    public sealed class TradeInTestParameter
    {
        public TradeInTestParameter(int id, bool tested)
        {
            Id = id;
            Tested = tested;
        }

        public int Id { get; }

        public bool Tested { get; }
    }
}