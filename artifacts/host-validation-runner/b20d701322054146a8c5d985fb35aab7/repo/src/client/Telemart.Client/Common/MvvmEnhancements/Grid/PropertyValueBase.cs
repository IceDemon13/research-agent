using DevExpress.Mvvm;

namespace Telemart.Client.Common.MvvmEnhancements.Grid
{
    public abstract class PropertyValueBase<T> : BindableBase, ITrackableValue
    {
        protected PropertyValueBase(T value)
        {
            Value = value;
            Origin = value;
        }

        public T Value
        {
            get { return GetProperty(() => Value); }
            set { SetProperty(() => Value, value, () => { RaisePropertiesChanged(nameof(IsChangedToValue), nameof(IsChangedToEmpty), nameof(IsChangedFromEmpty), nameof(IsChanged)); }); }
        }

        public virtual bool IsChangedToValue => Origin != null && Value != null && !Value.Equals(Origin);

        public virtual bool IsChangedToEmpty => Origin != null && Value == null;

        public virtual bool IsChangedFromEmpty => Origin == null && Value != null;

        public bool IsChanged => IsChangedFromEmpty || IsChangedToEmpty || IsChangedToValue;

        protected T Origin { get; set; }

        public virtual bool IsEmpty()
        {
            return Value == null;
        }

        public void ApplyChanges()
        {
            Origin = Value;
        }
    }
}