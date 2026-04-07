using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.Style.Editors
{
    public class MultiComboBoxValue : TelemartViewItemBase, IChangeTracking
    {
        private readonly List<object> _oldSelectedValues;

        public MultiComboBoxValue(
            IReadOnlyCollection<ComboBoxItem> availValues,
            IReadOnlyCollection<ComboBoxItem> selectedValues,
            bool isReadOnly = false)
        {
            AvailValues = availValues;
            SelectedValues = selectedValues.Cast<object>().ToList();
            _oldSelectedValues = selectedValues.Cast<object>().ToList();
            IsReadOnly = isReadOnly;
        }

        public List<object> SelectedValues
        {
            get => GetProperty(() => SelectedValues);
            set => SetProperty(() => SelectedValues, value, () => RaisePropertyChanged(nameof(IsChanged)));
        }

        public bool IsReadOnly { get; }

        public IReadOnlyCollection<ComboBoxItem> AvailValues { get; }

        public void AcceptChanges()
        {
        }

        public bool IsChanged => (SelectedValues?.Any() != true && _oldSelectedValues.Any())
                                 || SelectedValues?.Count != _oldSelectedValues.Count
                                 || SelectedValues?.Any(x => !_oldSelectedValues.Contains(x)) == true;
    }
}