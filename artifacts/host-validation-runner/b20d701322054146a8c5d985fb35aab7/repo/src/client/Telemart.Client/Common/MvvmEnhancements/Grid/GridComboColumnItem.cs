using System;
using System.Collections;

namespace Telemart.Client.Common.MvvmEnhancements.Grid
{
    public class GridComboColumnItem : GridColumnItem
    {
        public GridComboColumnItem(string fieldName, string header, bool fixedWidth, bool editable, bool isMultiValue, string regex, IList source, bool manualInput)
            : base(fieldName, header, fixedWidth, editable)
        {
            IsMultiValue = isMultiValue;
            Source = source;
            Regex = regex;
            ManualInput = manualInput;

            ButtonSource = manualInput ? new[] { (object)null } : Array.Empty<object>();
        }

        public bool IsMultiValue { get; }

        public string Regex { get; }

        public IList Source { get; }

        public object[] ButtonSource { get; }

        public bool ManualInput { get; }
    }
}