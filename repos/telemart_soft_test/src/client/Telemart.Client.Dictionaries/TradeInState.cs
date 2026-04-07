namespace Telemart.Client.Dictionaries
{
    public sealed class TradeInState : DictionaryItem
    {
        private const int NewId = 1;
        private const int EvaluatedId = 2;
        private const int CompletedId = 3;
        private const int CanceledId = 4;
        private const int ReceivedId = 5;

        private TradeInState(int id, string name)
            : base(id, name, true)
        {
        }

        public static TradeInState New { get; } = new TradeInState(NewId, "Новая");

        public static TradeInState Evaluated { get; } = new TradeInState(EvaluatedId, "Оценена");

        public static TradeInState Completed { get; } = new TradeInState(CompletedId, "Завершена");

        public static TradeInState Canceled { get; } = new TradeInState(CanceledId, "Отменена");

        public static TradeInState Received { get; } = new TradeInState(ReceivedId, "Принята");
    }
}