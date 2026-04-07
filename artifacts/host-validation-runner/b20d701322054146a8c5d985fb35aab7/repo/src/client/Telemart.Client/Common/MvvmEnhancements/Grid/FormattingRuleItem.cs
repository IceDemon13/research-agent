namespace Telemart.Client.Common.MvvmEnhancements.Grid
{
    public class FormattingRuleItem
    {
        public FormattingRuleItem(string fieldName, string expression, bool applyToRow)
        {
            Expression = expression;
            ApplyToRow = applyToRow;
            FieldName = fieldName;
        }

        public string Expression { get; }

        public bool ApplyToRow { get; }

        public string FieldName { get; }

        public override string ToString()
        {
            return $"{FieldName} => {Expression}";
        }
    }
}