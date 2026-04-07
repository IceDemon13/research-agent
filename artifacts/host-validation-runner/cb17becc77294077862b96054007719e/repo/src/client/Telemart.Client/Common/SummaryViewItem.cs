using DevExpress.Mvvm;

namespace Telemart.Client.Common
{
    public sealed class SummaryViewItem : BindableBase
    {
        public const int NormalLevel = 0;
        public const int GreenLevel = 1;
        public const int RedLevel = 2;

        public SummaryViewItem(string key, string value, int level = 0)
        {
            Key = key;
            Value = value;
            Level = level;
        }

        public string Key
        {
            get { return GetProperty(() => Key); }
            private set { SetProperty(() => Key, value); }
        }

        public string Value
        {
            get { return GetProperty(() => Value); }
            private set { SetProperty(() => Value, value); }
        }

        public int Level
        {
            get { return GetProperty(() => Level); }
            private set { SetProperty(() => Level, value); }
        }
    }
}
