namespace Telemart.Client.Dictionaries
{
    public class AssemblyFullRuleOperation : DictionaryItem
    {
        private const int EqualsId = 1;
        private const int NotEqualsId = 2;
        private const int MissingId = 3;
        private const int PresentId = 4;

        public AssemblyFullRuleOperation(int id, string name, bool canCompareValues)
            : base(id, name, true)
        {
            CanCompareValues = canCompareValues;
        }

        public static AssemblyFullRuleOperation EqualsValues { get; } = new AssemblyFullRuleOperation(EqualsId, "=", true);

        public static AssemblyFullRuleOperation NotEqualsValues { get; } = new AssemblyFullRuleOperation(NotEqualsId, "!=", true);

        public static AssemblyFullRuleOperation Missing { get; } = new AssemblyFullRuleOperation(MissingId, "Отсутствует", false);

        public static AssemblyFullRuleOperation Present { get; } = new AssemblyFullRuleOperation(PresentId, "Присутствует", false);

        public bool CanCompareValues { get; }
    }
}
