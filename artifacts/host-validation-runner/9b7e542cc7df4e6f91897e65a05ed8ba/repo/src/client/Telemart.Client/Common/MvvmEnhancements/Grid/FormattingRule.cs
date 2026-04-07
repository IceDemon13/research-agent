using System.Drawing;

namespace Telemart.Client.Common.MvvmEnhancements.Grid
{
    public sealed class FormattingRule : FormattingRuleItem
    {
        public FormattingRule(
            string fieldName,
            string expression,
            bool applyToRow,
            PropertyChangeType changeType,
            KnownColor? foregroundColor = default)
            : base(fieldName, expression, applyToRow)
        {
            ChangeType = changeType;
            ForegroundColor = foregroundColor;
        }

        public PropertyChangeType ChangeType { get; }

        public KnownColor? ForegroundColor { get; }

        public override string ToString()
        {
            return $"{FieldName} => {Expression}";
        }
    }
}