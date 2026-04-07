namespace Telemart.Client.Dictionaries
{
    public sealed class CallState : DictionaryItem
    {
        public const int NewId = 1;
        public const int SolvedId = 2;
        public const int NotSolvedId = 3;
        public const int NotReachedId = 4;
        public const int CanceledId = 5;

        private CallState(int id, string name, bool active = true)
            : base(id, name, active)
        {
        }

        public static CallState New { get; } = new CallState(NewId, "Новый");

        public static CallState Solved { get; } = new CallState(SolvedId, "Решили");

        public static CallState NotSolved { get; } = new CallState(NotSolvedId, "Не решили");

        public static CallState NotReached { get; } = new CallState(NotReachedId, "Не дозвонились");

        public static CallState Canceled { get; } = new CallState(CanceledId, "Отменен");
    }
}
