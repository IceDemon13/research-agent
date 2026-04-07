using System.Collections.ObjectModel;
using Telemart.Client.Common.MvvmEnhancements;

namespace Telemart.Client.ViewModels.Dialogs
{
    public class ChangeItemParameter
    {
        public ChangeItemParameter(ReadOnlyObservableCollection<ComboBoxItem> items, string oldItem, string title)
        {
            Items = items;
            Title = title;
            OldItem = oldItem;
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Items { get; set; }

        public string Title { get; set; }

        public string OldItem { get; }
    }
}
