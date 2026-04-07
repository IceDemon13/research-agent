using System.ComponentModel;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.Style.Editors
{
    public class ValueWrapper<T> : TelemartViewItemBase, IChangeTracking
    {
        private readonly T _oldValue;

        public ValueWrapper(T value, bool isReadOnly = false)
        {
            _oldValue = value;
            Value = value;
            IsReadOnly = isReadOnly;
        }

        public T Value
        {
            get => GetProperty(() => Value);
            set => SetProperty(() => Value, value, () => RaisePropertyChanged(nameof(IsChanged)));
        }

        public bool IsReadOnly { get; }

        public void AcceptChanges()
        {
        }

        public bool IsChanged => !Equals(Value, _oldValue);
    }
}