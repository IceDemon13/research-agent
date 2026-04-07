using DevExpress.Mvvm;

namespace Telemart.Client.Common.MvvmEnhancements.Grid
{
    public class CheckableItem<T> : BindableBase
    {
        public CheckableItem(T item, bool isChecked = false)
        {
            Item = item;
            IsChecked = isChecked;
        }

        public T Item
        {
            get { return GetProperty(() => Item); }
            set { SetProperty(() => Item, value); }
        }

        public bool IsChecked
        {
            get { return GetProperty(() => IsChecked); }
            set { SetProperty(() => IsChecked, value); }
        }
    }
}
