namespace Telemart.Client.Dictionaries
{
    public sealed class BacklogTaskState : DictionaryItem
    {
        private const int IdeaId = 1;
        private const int SpecifyId = 2;
        private const int FormulatedId = 3;
        private const int DocumentedId = 4;
        private const int InProgressId = 5;
        private const int CompletedId = 6;
        private const int CanceledId = 7;
        private const int RealizedId = 9;

        private BacklogTaskState(int id, string name, bool canChangeResponsible, bool canChangeEstimate)
            : base(id, name, true)
        {
            CanChangeResponsible = canChangeResponsible;
            CanChangeEstimate = canChangeEstimate;
        }

        public static BacklogTaskState Idea { get; } = new BacklogTaskState(IdeaId, "Идея", true, false);

        public static BacklogTaskState Specify { get; } = new BacklogTaskState(SpecifyId, "Уточняется", true, false);

        public static BacklogTaskState Formulated { get; } = new BacklogTaskState(FormulatedId, "Сформулирована", true, true);

        public static BacklogTaskState Documented { get; } = new BacklogTaskState(DocumentedId, "Есть ТЗ", true, true);

        public static BacklogTaskState InProgress { get; } = new BacklogTaskState(InProgressId, "В работе", false, false);

        public static BacklogTaskState Completed { get; } = new BacklogTaskState(CompletedId, "Готова", false, false);

        public static BacklogTaskState Canceled { get; } = new BacklogTaskState(CanceledId, "Отменена", false, false);

        public static BacklogTaskState Realized { get; } = new BacklogTaskState(RealizedId, "Реализована", false, false);

        public bool CanChangeResponsible { get; }

        public bool CanChangeEstimate { get; }
    }
}