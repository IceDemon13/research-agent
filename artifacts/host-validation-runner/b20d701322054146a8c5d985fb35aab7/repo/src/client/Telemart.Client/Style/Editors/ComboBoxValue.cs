using System.Collections.Generic;
using System.ComponentModel;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.Style.Editors
{
    public class ComboBoxValue : TelemartViewItemBase, IChangeTracking
    {
        private readonly ComboBoxItem? _oldValue;

        public ComboBoxValue(
            IReadOnlyCollection<ComboBoxItem> availValues,
            ComboBoxItem? selectedValue,
            bool isReadOnly = false)
        {
            AvailValues = availValues;
            SelectedValue = selectedValue;
            IsReadOnly = isReadOnly;
            _oldValue = selectedValue;
        }

        public ComboBoxItem? SelectedValue
        {
            get => GetProperty(() => SelectedValue);
            set => SetProperty(() => SelectedValue, value, () => RaisePropertyChanged(nameof(IsChanged)));
        }

        public bool IsReadOnly { get; }

        public IReadOnlyCollection<ComboBoxItem> AvailValues { get; }

        public void AcceptChanges()
        {
        }

        public bool IsChanged => SelectedValue != _oldValue;
    }
}