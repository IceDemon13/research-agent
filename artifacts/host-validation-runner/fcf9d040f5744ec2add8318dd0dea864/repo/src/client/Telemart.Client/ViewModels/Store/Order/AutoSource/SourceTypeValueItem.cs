using DevExpress.Mvvm;

namespace Telemart.Client.ViewModels.Store.Order.AutoSource
{
    public sealed class SourceTypeValueItem : BindableBase
    {
        public SourceTypeValueItem(string sourceType, bool value, int position, string name, string description)
        {
            SourceType = sourceType;
            Name = name;
            Value = value;
            Position = position;
            Description = description;
        }

        public string SourceType
        {
            get { return GetProperty(() => SourceType); }
            set { SetProperty(() => SourceType, value); }
        }

        public bool Value
        {
            get { return GetProperty(() => Value); }
            set { SetProperty(() => Value, value); }
        }

        public int Position
        {
            get { return GetProperty(() => Position); }
            set { SetProperty(() => Position, value); }
        }

        public string Name
        {
            get { return GetProperty(() => Name); }
            set { SetProperty(() => Name, value); }
        }

        public string Description
        {
            get { return GetProperty(() => Description); }
            set { SetProperty(() => Description, value); }
        }
    }
}