namespace Telemart.Client.Dictionaries
{
    public class ComplaintState : DictionaryItem
    {
        private const int NewId = 1;
        private const int AcceptedId = 2;
        private const int CancelledId = 3;
        private const int DeclinedId = 4;

        public ComplaintState(int id, string name, string descriptionTitle, bool isFinished, int position)
            : base(id, name, true)
        {
            DescriptionTitle = descriptionTitle;
            IsFinished = isFinished;
            Position = position;
        }

        public static ComplaintState New { get; } = new ComplaintState(NewId, "Новая", string.Empty, false, 1);

        public static ComplaintState Accepted { get; } = new ComplaintState(AcceptedId, "Урегулирована", "Решение", true, 2);

        public static ComplaintState Cancelled { get; } = new ComplaintState(CancelledId, "Отклонена", "Обоснование", true, 4);

        public static ComplaintState Declined { get; } = new ComplaintState(DeclinedId, "Не урегулирована", "Причина", true, 3);

        public string DescriptionTitle { get; }

        public bool IsFinished { get; }

        public int Position { get; }
    }
}
