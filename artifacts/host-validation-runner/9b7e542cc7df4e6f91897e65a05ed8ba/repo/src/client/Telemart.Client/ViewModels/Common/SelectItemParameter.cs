using System.Collections.Generic;
using Telemart.Client.Common.MvvmEnhancements;

namespace Telemart.Client.ViewModels.Common
{
    public sealed class SelectItemParameter
    {
        public SelectItemParameter(ICollection<ComboBoxItem> items, string title, string itemName, ComboBoxItem? selectedItem = null)
        {
            Items = items;
            Title = title;
            ItemName = itemName;
            SelectedItem = selectedItem;
        }

        public ICollection<ComboBoxItem> Items { get; }

        public ComboBoxItem? SelectedItem { get; }

        public string Title { get; }

        public string ItemName { get; }
    }
}