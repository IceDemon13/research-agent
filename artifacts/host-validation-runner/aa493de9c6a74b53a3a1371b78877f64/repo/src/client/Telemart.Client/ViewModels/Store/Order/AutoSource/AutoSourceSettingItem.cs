using DevExpress.Mvvm;

namespace Telemart.Client.ViewModels.Store.Order.AutoSource
{
    public sealed class AutoSourceSettingItem : BindableBase
    {
        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public string CodeName
        {
            get { return GetProperty(() => CodeName); }
            set { SetProperty(() => CodeName, value); }
        }

        public string DisplayName
        {
            get { return GetProperty(() => DisplayName); }
            set { SetProperty(() => DisplayName, value); }
        }

        public string Description
        {
            get { return GetProperty(() => Description); }
            set { SetProperty(() => Description, value); }
        }

        public string Value
        {
            get { return GetProperty(() => Value); }
            set { SetProperty(() => Value, value, () => RaisePropertyChanged(nameof(ValueBoolTrue))); }
        }

        public bool Active
        {
            get { return GetProperty(() => Active); }
            set { SetProperty(() => Active, value); }
        }

        public bool ValueBoolTrue => bool.TryParse(Value, out bool valueBoolEnabled) && valueBoolEnabled;
    }
}